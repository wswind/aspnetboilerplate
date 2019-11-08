﻿using System.Collections.Generic;
using System.Linq;
using Abp.BackgroundJobs;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.MultiTenancy;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Abp.Timing;

namespace Abp.Authorization.Users
{
    public class UserTokenExpirationWorker<TTenant, TUser> : PeriodicBackgroundWorkerBase
        where TTenant : AbpTenant<TUser>
        where TUser : AbpUserBase
    {
        private const int IntervalInMilliseconds = 1 * 60 * 60 * 1000; // 1 hour

        private readonly IRepository<UserToken, long> _userTokenRepository;
        private readonly IRepository<TTenant> _tenantRepository;
        private readonly IBackgroundJobConfiguration _backgroundJobConfiguration;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public UserTokenExpirationWorker(
            AbpTimer timer,
            IRepository<UserToken, long> userTokenRepository,
            IBackgroundJobConfiguration backgroundJobConfiguration, 
            IUnitOfWorkManager unitOfWorkManager, 
            IRepository<TTenant> tenantRepository)
            : base(timer)
        {
            _userTokenRepository = userTokenRepository;
            _backgroundJobConfiguration = backgroundJobConfiguration;
            _unitOfWorkManager = unitOfWorkManager;
            _tenantRepository = tenantRepository;

            Timer.Period = GetTimerPeriod();
        }

        private int GetTimerPeriod()
        {
            if (_backgroundJobConfiguration.CleanUserTokenPeriod.HasValue)
            {
                return _backgroundJobConfiguration.CleanUserTokenPeriod.Value;
            }

            return IntervalInMilliseconds;
        }

        protected override void DoWork()
        {
            List<int> tenantIds;
            var utcNow = Clock.Now.ToUniversalTime();

            using (var uow = _unitOfWorkManager.Begin())
            {
                using (_unitOfWorkManager.Current.SetTenantId(null))
                {
                    _userTokenRepository.Delete(t => t.ExpireDate <= utcNow);
                    tenantIds = _tenantRepository.GetAll().Select(t => t.Id).ToList();
                    uow.Complete();
                }
            }

            foreach (var tenantId in tenantIds)
            {
                using (var uow = _unitOfWorkManager.Begin())
                {
                    using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                    {
                        _userTokenRepository.Delete(t => t.ExpireDate <= utcNow);
                        uow.Complete();
                    }
                }
            }
        }
    }
}
