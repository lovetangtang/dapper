using DapperCore;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DapperExtensions;
using DapperCore.BLL;
using System.Transactions;
namespace DapperExample
{
    class Program
    {
        static void Main(string[] args)
        {
           

            //查询
            Stopwatch swSearch = new Stopwatch();
            swSearch.Start();

            string sql = "select * from UserInfo";
            var list = DapperSql.GetInfoList<UserInfo>(sql);


            swSearch.Stop();
            TimeSpan tsSearch = swSearch.Elapsed;
            Console.WriteLine("查询整表总共花费{0}ms.", tsSearch.TotalMilliseconds);

            //分页查询
            Stopwatch swPage = new Stopwatch();
            swPage.Start();

            string sqlPage = "select * from UserInfo";
            var listPage = DapperSql.GetPageList<UserInfo>(sqlPage, "order by ID", 3, 10);

            swPage.Stop();
            TimeSpan tsPage = swPage.Elapsed;
            Console.WriteLine("分页查询总共花费{0}ms.", tsPage.TotalMilliseconds);


            //添加
            string sqlInsert = @"INSERT INTO [UserInfo]
                                          ([username]
                                          ,[email]
                                          ,[sex]
                                          ,[city]
                                          ,[sign]
                                          ,[experience]
                                          ,[ip]
                                          ,[logins]
                                          ,[joinTime])
                                    VALUES
                                          (@username
                                          ,@email
                                          ,@sex
                                          ,@city
                                          ,@sign
                                          ,@experience
                                          ,@ip
                                          ,@logins
                                          ,@joinTime)";

            for (int i = 0; i < 20; i++)
            {

                var success = DapperSql.UpdateSql(sqlInsert, new UserInfo { Username = "堂堂", Email = "764053787@qq.com",  City = "重庆", Sign = "en" });
                Console.WriteLine(success);
            }

            //删除
            string sqlDelete = "delete UserInfo where ID=@ID";
            var successDel = DapperSql.UpdateSql(sqlDelete, new { ID = 123 });


            //修改
            for (int i = 50; i < 100; i++)
            {
                string sqlUpdate = "update UserInfo set username=@username where ID=@ID";
                var successUpdate = DapperSql.UpdateSql(sqlUpdate, new { ID = i, username = "王安石" + i });
                Console.WriteLine(successUpdate);
            }

            //批量增加
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<UserInfo> listUser = new List<UserInfo>();
            for (int i = 0; i < 1000000; i++)
            {
                listUser.Add(new UserInfo { Username = "堂堂批量新增", Email = "764053787@qq.com", Sex = "1", City = "重庆", Sign = "en" });
            }
            var s_1 = DapperSql.UpdateSql(sqlInsert, listUser);
            Console.WriteLine(s_1);
            sw.Stop();
            TimeSpan ts2 = sw.Elapsed;
            Console.WriteLine("批量新增总共花费{0}ms.", ts2.TotalMilliseconds);


            //批量修改
            Stopwatch sw1 = new Stopwatch();
            sw1.Start();
            var list_Update = DapperSql.GetInfoList<UserInfo>("select * from UserInfo where ID>0 and ID<60").ToList();
            List<UserInfo> listExecUpdate = new List<UserInfo>();
            for (int i = 0; i < list_Update.Count; i++)
            {
                UserInfo t = list_Update[i];
                t.Username = "批量修改";
                listExecUpdate.Add(t);
            }
            string sqlUpdates = @"UPDATE [UserInfo]
                                  SET [username] = @username
                                     ,[email] = @email
                                     ,[sex] = @sex
                                     ,[city] = @city
                                     ,[sign] = @sign
                                     ,[experience] = @experience
                                     ,[ip] = @ip
                                     ,[logins] = @logins
                                     ,[joinTime] = @joinTime where ID=@ID";
            var s = DapperSql.UpdateSql(sqlUpdates, listExecUpdate);
            sw1.Stop();
            TimeSpan ts3 = sw1.Elapsed;
            Console.WriteLine("批量修改总共花费{0}ms.", ts3.TotalMilliseconds);



            //dapper实体增删改查方法，原生代码使用
            using (SqlConnection cn = DapperSql.getCon())
            {
                cn.Open();
                IList<ISort> sort = new List<ISort>();
                sort.Add(new Sort { PropertyName = "ID", Ascending = false });
                var person = cn.GetPage<UserInfo>(null, sort, 1, 10).ToList().ToList();//分页查询
                var person1 = person.Select(e => new { e.ID, e.Username });//分页查询查询某列
                var single = cn.Get<UserInfo>(1);//获取单个实体数据

                cn.Insert<UserInfo>(new UserInfo { Username = "阿帅帅", Email = "764053787@qq.com", Sex = "1", City = "重庆", Sign = "en" });//新增

                cn.Update<UserInfo>(new UserInfo { ID = 2, Username = "我是第一" });//修改

                cn.Delete<UserInfo>(new { ID = 12 });//删除

                cn.Close();
            }

            var dbset01 = ContextFactory.CreateDbSet<UserInfo>();
            var listAll = dbset01.GetAll().ToList();

            //多表事物操作
            using (var tranScope = new TransactionScope())
            {
                try
                {
                    UserInfo obj = new UserInfo { Username = "大明" };
                    var list01 = dbset01.Insert(obj);
                    tranScope.Complete();
                }
                catch (Exception ex)
                {
                    //任务出差 就不会提交 自动回滚
                    throw ex;
                }
            }

            //Action BookAction = new Action(insert);
            //var suc = ContextFactory.Submit(BookAction);
            //dapper实体操作，封装代码调用
            BLL_UserInfo bll = new BLL_UserInfo();
            bll.Test();

            return;
            //var usr = DapperSql.GetById<UserInfo>(1);
        }
    }
}
