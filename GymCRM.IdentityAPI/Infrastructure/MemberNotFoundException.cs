using System.Data.Common;
using Npgsql;

namespace GymCRM.IdentityAPI.Models;

public class MemberNotFoundException : DbException
{
    public MemberNotFoundException() { }
    public MemberNotFoundException(string message) : base(message) { }
    public MemberNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}