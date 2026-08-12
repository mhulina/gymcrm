using System.Data.Common;

namespace GymCRM.IdentityAPI.Models.Exceptions;

public class MemberNotFoundException : DbException
{
    public MemberNotFoundException() { }
    public MemberNotFoundException(string message) : base(message) { }
    public MemberNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}