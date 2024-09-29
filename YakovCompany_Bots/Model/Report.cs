using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YakovCompany_bot.Model
{
    public class Report
    {
        public long chatid { get; set; }
        public string Nickname { get; set; }

        public string FIO { get; set; }

        public string NameOrg { get; set; }
        public List<Question> questions { get; set; } = new List<Question>();

    }
}
