using System;
using HttPServer.Core.Attributes;
using HttPServer.Framework.Core.Abstracts;
using HttPServer.Framework.Core.Response;
using HttPServer.Suka;
using ORMLibrary;

[Endpoint]
public class UserEndpoint : BaseEndpoint
{

    [HttpGet("/users/")]
    public IResponse GetUsers()
    {
        var orm = new ORM(Global.Settings.Model.ConnectionString);

        var data = new
        {
            Users = new
            {
                Items = orm.ReadByAll<Users>()
            }
        };

        return Page("/users/index.html", data);
    }
}