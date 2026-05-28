using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Camefor.Tools.Util
{
    /// <summary>
    /// 描   述  ： to trigger an event at a specific time                         
    /// 版   本  ： V1.0.0                            
    /// 创 建 人 ： rhyswang                                  
    /// 日    期 ：                         
    /// 创 建 人 ：                                   
    /// 创建时间 ：                                  
    /// 修 改 人 ：                                   
    /// 修改时间 ：                                   
    /// 修改描述 ：                                   
    /// </summary> 
    public class DailyTrigger
    {
        readonly TimeSpan triggerHour;

        public DailyTrigger(int hour, int minute = 0, int second = 0)
        {
            triggerHour = new TimeSpan(hour, minute, second);
            InitiateAsync();
        }

        async void InitiateAsync()
        {
            while (true)
            {
                var triggerTime = DateTime.Today + triggerHour - DateTime.Now;
                if (triggerTime < TimeSpan.Zero)
                    triggerTime = triggerTime.Add(new TimeSpan(24, 0, 0));
                await Task.Delay(triggerTime);
                OnTimeTriggered?.Invoke();
            }
        }

        public event Action OnTimeTriggered;

        /**
         * 
         *   var trigger = new DailyTrigger(16); // every day at 4:00pm

    trigger.OnTimeTriggered += () => 
    {
        // Whatever
    };  

    Console.ReadKey();
         * 
         * **/
    }
}
