using Microsoft.AspNetCore.Identity;

namespace ServiceCenter.Models;

public class Role : IdentityRole<int>
{
    public Role() : base() { }
    public Role(string name) : base(name) { }
}
