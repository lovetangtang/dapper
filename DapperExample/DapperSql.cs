using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions;

namespace DapperExample
{
    /// <summary>
    /// 数据操作类
    /// </summary>
    public static class DapperSql
    {
        //获取连接字符串
        public readonly static string sqlconnct = "server=.;database=GridDemo;uid=sa;pwd=123456;MultipleActiveResultSets=True;App=EntityFramework";// ConfigurationManager.AppSettings["strconn"]; 
        //初始化连接对象
        public static SqlConnection conn = new SqlConnection(sqlconnct);
        public static SqlConnection getCon()
        {
            return new SqlConnection(sqlconnct);
        }
        //public static DapperSql()
        //{
        //    conn = new SqlConnection(sqlconnct);
        //}
        /// <summary>
        /// 打开数据库连接
        /// </summary>
        private static void OpenConnect()
        {
            if (conn.State == ConnectionState.Closed)
            {
                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        private static void CloseConnect()
        {
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
        }
        /// <summary>
        /// 查询语句，必要形参sql语句或存储过程名称，后面参数用于扩展可以不写，若两边有参数中间用null占位
        /// </summary>
        /// <typeparam name="T">强类型的类</typeparam>
        /// <param name="sql">sql执行语句或存储过程名称</param>
        /// <param name="parameter">sql参数，可匿名类型，可对象类型</param>
        /// <param name="transaction">执行事务</param>
        /// <param name="buffered"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns>对象集合</returns>
        public static IEnumerable<T> GetInfoList<T>(string sql, object parameter = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
        {
            try
            {
                OpenConnect();
                //可以让结果转换成其他集合形式 例：list、array等集合，方法： ToList<>、ToArray<>
                IEnumerable<T> result = conn.Query<T>(sql, parameter, transaction, buffered, commandTimeout, commandType);
                CloseConnect();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 分页查询数据
        /// </summary>
        /// <param name="cmdText">查询语句</param>
        /// <param name="orderStr">排序</param>
        /// <param name="currentPageIndex">页码</param>
        /// <param name="pageSize">每页行数</param>
        /// <param name="parameter"></param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetPageList<T>(string cmdText, string orderStr, int currentPageIndex, int pageSize, object parameter = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
        {

            try
            {
                OpenConnect();
                int endRowNum = currentPageIndex * pageSize;
                int startRowNum = endRowNum - pageSize + 1;
                string rowNumStr = "ROWNUM=Row_Number() OVER(" + orderStr + ")";
                cmdText = "select * from ( select " + rowNumStr + " ,* from (" + cmdText + ")TEMP)TEMP WHERE ROWNUM>=" + startRowNum + " AND ROWNUM<=" + endRowNum;
                //cmdText = cmdText.Replace(",'END'", "").Replace("'BEGIN'", rowNumStr);
                //cmdText = "SELECT * FROM (" + cmdText + ") TEMP WHERE ROWNUM>=" + startRowNum + " AND ROWNUM<=" + endRowNum;
                //可以让结果转换成其他集合形式 例：list、array等集合，方法： ToList<>、ToArray<>
                IEnumerable<T> result = conn.Query<T>(cmdText, parameter, transaction, buffered, commandTimeout, commandType);
                CloseConnect();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 插入、更新或删除语句，必要形参sql语句或存储过程名称，后面参数用于扩展可以不写，若两边有参数中间用null占位
        /// </summary>
        /// <typeparam name="T">强类型的类</typeparam>
        /// <param name="sql">sql执行语句或存储过程名称</param>
        /// <param name="parameter">sql参数，可匿名类型，可对象类型</param>
        /// <param name="transaction">执行事务</param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns>成功：true;失败：false</returns>
        public static bool UpdateSql(string sql, object parameter = null, IDbTransaction transaction = null, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
        {
            try
            {
                OpenConnect();
                int result = conn.Execute(sql, parameter, transaction, commandTimeout, commandType);
                CloseConnect();
                if (result > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 根据条件获取数据库表中列表数量,必要形参sql,后面参数用于扩展可以不写，若两边有参数中间用null占位
        /// </summary>
        /// <param name="sql">sql执行语句或存储过程名称</param>
        /// <param name="parameter">sql参数，可匿名类型，可对象类型</param>
        /// <param name="transaction"></param>
        /// <param name="buffered"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="commandType"></param>
        /// <returns></returns>
        public static int GetInfoCounts(string sql, object parameter = null, IDbTransaction transaction = null, bool buffered = true, int? commandTimeout = default(int?), CommandType? commandType = default(CommandType?))
        {
            try
            {
                OpenConnect();
                //注意：sql语句应该是这种形式 select count(*) as rows from table
                int result = conn.Query<int>(sql, parameter, transaction, buffered, commandTimeout, commandType).First();
                CloseConnect();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region 实体操作


        #endregion
    }
}
