using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OCF_Ws.WsCard;
namespace OCF_Ws.Model
{
    public class CardVisibility
    {
        public List<User> Users;
        public List<Group> Groups;
        public List<Office> Offices;
        public List<User> UsersCC;
        public List<Group> GroupsCC;
        public List<Office> OfficesCC;
    }
}
