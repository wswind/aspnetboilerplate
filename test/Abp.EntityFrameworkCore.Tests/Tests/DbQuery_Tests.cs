using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore.Tests.Domain;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Abp.EntityFrameworkCore.Tests.Tests
{
    public class DbQuery_Tests : EntityFrameworkCoreModuleTestBase
    {
        [Fact]
        public async Task DbQuery_Test()
        {
            await WithUnitOfWorkAsync(async () =>
             {
                 var blogViewRepository = Resolve<IRepository<BlogView>>();

                 var blogViews = await blogViewRepository.GetAll().AsNoTracking().ToListAsync();

                 blogViews.ShouldNotBeNull();
                 blogViews.ShouldContain(x => x.Name == "test-blog-1" && x.Url == "http://testblog1.myblogs.com");
             });
        }
    }
}