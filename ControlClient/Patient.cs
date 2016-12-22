using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ControlClient
{
    public class Patient
    {
        private static Patient _patient = null;
        public static Patient GetInstance()
        {
            if (_patient == null)
            {
                _patient =  new Patient();
            }
            return _patient;
        }

        public static void SetPatient(Patient p)
        {
            _patient = p;
        }
        public Doctor doctor { get; set; }
        public int id{get;set;}
        public String realname { get; set; }
    }
}
