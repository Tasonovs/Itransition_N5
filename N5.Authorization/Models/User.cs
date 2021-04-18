using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace N5.Authorization.Models
{
	public class User
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public string Password { get; set; }
		public DateTime RegistrationDate { get; set; }
		public DateTime LastLoginDate { get; set; }
		public bool IsBlocked { get; set; }
	}
}
