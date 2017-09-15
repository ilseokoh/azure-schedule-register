using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWebApp.Models
{
    public class Schedule
    {
        /// <summary>
        /// 스케줄 작업 이름 (Unique 해야함) 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// DateTime UTC 시간을 꼭 써야함. 
        /// </summary>
        public DateTime TriggerDateTimer { get; set; }

        /// <summary>
        /// Request URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Http or Https
        /// </summary>
        public string Type { get; set; }
    }
}
