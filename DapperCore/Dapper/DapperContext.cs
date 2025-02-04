﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DapperExtensions;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using System.Configuration;

namespace DapperCore
{
    /// <summary>
    /// 数据访问上下文
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public partial class DapperContext<TEntity> : IDataAccess<TEntity>, IDisposable where TEntity : class, ITEntity
    {

        #region 初始化参数
        /// <summary>
        /// 数据库连接实例 
        /// </summary>
        internal IDbConnection _Conn { get; set; }


        /// <summary>
        /// 通过数据库连接实例创建访问上下文
        /// </summary>
        /// <param name="conn"></param>
        public DapperContext(IDbConnection conn)
        {
            this._Conn = conn;
        }

        /// <summary>
        /// 通过连接字符串创建访问上下文
        /// </summary>
        /// <param name="connStr"></param>
        public DapperContext(string connStr)
        {
            this._Conn = new SqlConnection("server=.;database=GridDemo;uid=sa;pwd=123456;MultipleActiveResultSets=True;App=EntityFramework");
            //(ConfigurationManager.ConnectionStrings[connStr].ConnectionString);
        }

        /// <summary>
        /// 通过连接字符串创建访问上下文
        /// </summary>
        /// <param name="connStr"></param>
        public DapperContext()
        {
            this._Conn = new SqlConnection("server=.;database=GridDemo;uid=sa;pwd=123456;MultipleActiveResultSets=True;App=EntityFramework");
            //(ConfigurationManager.ConnectionStrings[connStr].ConnectionString);
        }


        #endregion

        #region 数据操作


        public TEntity Insert(TEntity entity)
        {
            try
            {
                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                var id = _Conn.Insert<TEntity>(entity);
                return id == null ? null : entity;
            }
            finally
            {
                _Conn.Close();
            }

        }

        public bool Insert(IEnumerable<TEntity> entitys)
        {
            if (_Conn.State == ConnectionState.Closed)
                _Conn.Open();
            IDbTransaction tran = _Conn.BeginTransaction();
            try
            {
                _Conn.Insert(entitys, transaction: tran);
                tran.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tran.Rollback();//事物回滚
                throw ex;
            }
            finally
            {
                _Conn.Close();
            }
        }


        public TEntity Update(TEntity entity)
        {
            try
            {
                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                var flag = _Conn.Update<TEntity>(entity);
                return flag ? entity : null;
            }
            finally
            {
                _Conn.Close();
            }
        }

        public bool Update(object id, object prams)
        {
            try
            {
                var tableName = typeof(TEntity).Name;//获取当前要更新的表名称
                if (typeof(TEntity).Name == prams.GetType().Name) throw new ArgumentException("参数不能是当前实体的强类型,否则更新会覆盖所有未赋值的字段");

                //获取指定的更新字段
                PropertyInfo[] fields = prams.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                //构建Sql语句
                var sqlBuilder = new StringBuilder("Update\0" + tableName + "\0set\0");

                foreach (var f in fields)
                {
                    //验证指定更新字段是否在表中存在    
                    var exist = typeof(TEntity).GetProperty(f.Name);
                    if (exist == null) throw new ArgumentException("指定的更新的字段在表中不存在,请检查!");

                    sqlBuilder.Append(f.Name + "=@" + f.Name + "\0");
                    if (fields.Count() > 1 && fields.Last() != f)
                        sqlBuilder.Append(",");
                }

                sqlBuilder.Append("where Id=" + "'" + id + "'");
                var sql = sqlBuilder.ToString();
                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                var succ = _Conn.Execute(sql, prams);
                return succ > 0;
            }
            finally
            {
                _Conn.Close();
            }
        }


        public bool Update(IEnumerable<TEntity> entitys)
        {
            if (_Conn.State == ConnectionState.Closed)
                _Conn.Open();

            IDbTransaction tran = _Conn.BeginTransaction();
            try
            {
                foreach (var item in entitys)
                {
                    _Conn.Update<TEntity>(item, transaction: tran);
                }
                tran.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tran.Rollback();//事物回滚
                throw ex;
            }
            finally
            {
                tran.Dispose();
                _Conn.Close();
            }
        }


        public bool Delete(object key)
        {
            try
            {
                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                TEntity item = _Conn.Get<TEntity>(key);
                if (item == null)
                    return false;
                return _Conn.Delete(item);
            }
            finally
            {
                _Conn.Close();
            }
        }


        public bool Delete(IEnumerable<object> keys)
        {
            if (_Conn.State == ConnectionState.Closed)
                _Conn.Open();
            IDbTransaction tran = _Conn.BeginTransaction();
            try
            {
                var tblName = typeof(TEntity).Name;
                keys = keys.Select(k => string.Format("'{0}'", k));
                var sql = "Delete From " + tblName + " where Id in (" + string.Join(",", keys) + ")";
                _Conn.Execute(sql, transaction: tran);
                tran.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tran.Rollback();//事物回滚
                throw ex;
            }
            finally
            {
                _Conn.Close();
            }
        }


        /// <summary>
        /// 逻辑删除数据
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool DeleteLogic(object key)
        {
            return this.Update(key, new { DataState = DataState.Deleted });
        }

        #endregion

        #region 数据查询
        /// <summary>
        /// 根据Id获取实体对象
        /// </summary>
        /// <param name="id"></param>
        /// <param name="queryState">默认返回正常数据,数据状态支持enum Flags位标志</param>
        /// <returns></returns>
        public TEntity GetById(object id, DataState queryState = DataState.Normal)
        {
            try
            {
                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                var item = _Conn.Get<TEntity>(id);
                if (item == null) return null;
                return item; //使用Flags位运算
            }
            finally
            {
                _Conn.Close();
            }
        }

        /// <summary>
        /// 获取数据表总项数
        /// </summary>
        /// <param name="expression">linq表达式 谓词</param>
        /// <returns></returns>
        public long GetCount(Expression<Func<TEntity, bool>> expression = null)
        {
            try
            {
                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                var predicate = DapperLinqBuilder<TEntity>.FromExpression(expression);
                return _Conn.Count<TEntity>(predicate);
            }
            finally
            {
                _Conn.Close();
            }
        }

        /// <summary>
        /// 获取结果集第一条数据
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="sortList"></param>
        /// <returns></returns>
        public TEntity GetFist(Expression<Func<TEntity, bool>> expression = null, object sortList = null)
        {
            try
            {
                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                var predicate = DapperLinqBuilder<TEntity>.FromExpression(expression);
                var sort = SortConvert(sortList);
                var data = _Conn.GetSet<TEntity>(predicate, sort, 0, 1);
                return data.FirstOrDefault();
            }
            finally
            {
                _Conn.Close();
            }
        }

        /// <summary>
        /// 查看指定的数据是否存在
        /// </summary>
        /// <param name="expression">linq表达式 谓词</param>
        /// <returns></returns>
        public bool Exists(Expression<Func<TEntity, bool>> expression)
        {
            var ct = this.GetCount(expression);
            return ct > 0;
        }

        /// <summary>
        /// 根据条件获取表数据
        /// </summary>
        /// <param name="expression">linq表达式</param>
        /// <returns></returns>
        public IEnumerable<TEntity> GetTableData(Expression<Func<TEntity, bool>> expression, object sortList = null)
        {
            try
            {

                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                IList<ISort> sort = SortConvert(sortList);//转换排序接口
                if (expression == null)
                {
                    //允许脏读
                    return _Conn.GetList<TEntity>(null, sort, transaction: _Conn.BeginTransaction(IsolationLevel.ReadUncommitted));//如果条件为Null 就查询所有数据
                }
                else
                {
                    var predicate = DapperLinqBuilder<TEntity>.FromExpression(expression);
                    return _Conn.GetList<TEntity>(predicate, sort, transaction: _Conn.BeginTransaction(IsolationLevel.ReadUncommitted));
                }
            }
            finally
            {
                _Conn.Close();
            }
        }

        /// <summary>
        /// 获取表的所有数据
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TEntity> GetAll()
        {
            return GetTableData(null);
        }

        /// <summary>
        /// 数据表 分页
        /// </summary>
        /// <param name="pageNum">指定页数 索引从0开始</param>
        /// <param name="pageSize">指定每页多少项</param>
        ///<param name="outTotal">输出当前表的总项数</param>
        /// <param name="expression">条件 linq表达式 谓词</param>
        /// <param name="sortList">排序字段</param>
        /// <returns></returns>
        public IEnumerable<TEntity> GetPageData(int pageNum, int pageSize, out long outTotal,
            Expression<Func<TEntity, bool>> expression = null, object sortList = null)
        {
            try
            {
                if (_Conn.State == ConnectionState.Closed)
                    _Conn.Open();
                IPredicateGroup predicate = DapperLinqBuilder<TEntity>.FromExpression(expression); //转换Linq表达式
                IList<ISort> sort = SortConvert(sortList);//转换排序接口
                var entities = _Conn.GetPage<TEntity>(predicate, sort, pageNum, pageSize, transaction: _Conn.BeginTransaction(IsolationLevel.ReadUncommitted));
                outTotal = _Conn.Count<TEntity>(null);
                return entities;
            }
            finally
            {
                _Conn.Close();
            }
        }


        #endregion

        #region 辅助方法
        /// <summary>
        /// 转换成Dapper排序方式
        /// </summary>
        /// <param name="sortList"></param>
        /// <returns></returns>
        private static IList<ISort> SortConvert(object sortList)
        {
            IList<ISort> sorts = new List<ISort>();
            if (sortList == null)
            {
                sorts.Add(Predicates.Sort<TEntity>(f => f.ID, false));//默认以开始时间 最早创建的时间 asc=flase 降序
                return sorts;
            }

            Type obj = sortList.GetType();
            var fields = obj.GetRuntimeFields();
            Sort s = null;
            foreach (FieldInfo f in fields)
            {
                s = new Sort();
                var mt = Regex.Match(f.Name, @"^\<(.*)\>.*$");
                s.PropertyName = mt.Groups[1].Value;
                s.Ascending = f.GetValue(sortList) == null ? true : (bool)f.GetValue(sortList);
                sorts.Add(s);
            }

            return sorts;
        }


        /// <summary>
        /// 释放对象
        /// </summary>
        public void Dispose()
        {
            this._Conn.Dispose();//释放数据连接
            this.Dispose();//交给GC释放
        }
        #endregion
    }
}
