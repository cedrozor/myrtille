/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2018 Cedric Coste
    Copyright(c) 2018 Paul Oliver

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/
using System.Data.Entity;
using System.Data.SqlServerCe;

namespace Myrtille.Enterprise
{
    public class MyrtilleEnterpriseDBContext : DbContext
    {
        public MyrtilleEnterpriseDBContext() : 
            base("name=enterpriseDBConnection")
            //base(new SqlCeConnection(@"Data Source=|DataDirectory|MyrtilleEnterprise.sdf;Persist Security Info=False;"), contextOwnsConnection: true)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<MyrtilleEnterpriseDBContext, MigrationConfiguration>());
        }

        public virtual DbSet<Session> Session { get; set; }
        public virtual DbSet<SessionGroup> SessionGroup { get; set; }
        public virtual DbSet<Host> Host { get; set; }
        public virtual DbSet<HostAccessGroups> HostAccessGroups { get; set; }
        public virtual DbSet<SessionHostCredential> SessionHostCredentials { get; set; }
    }
}