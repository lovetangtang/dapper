using System;
using System.Collections.Generic;
using System.Text;

namespace DapperCore
{
    [Serializable]
    public class UserInfo : ITEntity
    {
        private int id;
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        private string username;
        public string Username
        {
            get { return username; }
            set { username = value; }
        }

        private string email;
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        private string sex;
        public string Sex
        {
            get { return sex; }
            set { sex = value; }
        }

        private string city;
        public string City
        {
            get { return city; }
            set { city = value; }
        }

        private string sign;
        public string Sign
        {
            get { return sign; }
            set { sign = value; }
        }

        private string experience;
        public string Experience
        {
            get { return experience; }
            set { experience = value; }
        }

        private string ip;
        public string Ip
        {
            get { return ip; }
            set { ip = value; }
        }

        private string logins;
        public string Logins
        {
            get { return logins; }
            set { logins = value; }
        }
        public string joinTime { get; set; }
    }
}