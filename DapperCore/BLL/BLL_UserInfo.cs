using DapperExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperCore.BLL
{
    public partial class BLL_UserInfo : DapperContext<UserInfo>
    {


        public void Test()
        {
            UserInfo entity = new UserInfo { ID = 6, Username = "恩恩额" };
            //根据实体主键删除
            this.Delete(entity);

            //根据主键ID删除
            this.Delete(1);

            //增加
            this.Insert(entity);

            //更新
            var result = this.Update(entity);

            //根据主键返回实体
            entity = this.GetById(1710437);

            //返回 行数
            long c = this.GetCount(e => e.ID == 1710437);

            //IList<ISort> sort = new List<ISort>();
            //sort.Add(new Sort { PropertyName = "ID", Ascending = false });
            //条件查询,ID倒序
            var list1 = this.GetTableData(e => e.ID > 10 && e.ID < 100, new { ID = false }).ToList();

            //查询所有
            var list = this.GetTableData(null).ToList();

            //分页查询
            long allRowsCount = 0;
            var listPage = this.GetPageData(1, 10, out allRowsCount, e => e.ID > 10 && e.ID < 100);
        }
    }
}
