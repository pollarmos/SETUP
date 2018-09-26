using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;

namespace SETUP
{
    public class Mill
    {
        public static Mill AppMill;//MainWindow의 정적인스턴스 선언

        public double PORMaxTension;
        public double TRMaxTension;
        public double MaxRollForce;
        public double MaxSpeed;
        public double DiameterOfWorkRoll;
        public double ClassThick;
        public double V1passH;
        public double V1passC;
        public double Vmpass;
        public double Vfinal;
        public Mill()
        {
            AppMill = this;
        }
        public void ReadMillData()
        {
            string fileName = "SETUP.Resources.mill.txt";
            Assembly a = Assembly.GetExecutingAssembly();
            string[] str;
            double[] data = new double[5];
            int i = 0;
            StreamReader sr = new StreamReader(a.GetManifestResourceStream(fileName));
            while (sr.Peek() > -1)
            {
                str = sr.ReadLine().Split(',');
                data[i]= double.Parse(str[1]);
                i++;
            }
            sr.Close();
            PORMaxTension = data[0];
            TRMaxTension = data[1];
            MaxRollForce = data[2];
            MaxSpeed = data[3];
            DiameterOfWorkRoll = data[4];
        }
        public void ReadSpeed()
        {
            string fileName = "SETUP.Resources.speed.txt";
            Assembly a = Assembly.GetExecutingAssembly();
            string[] str = null;
            StreamReader sr = new StreamReader(a.GetManifestResourceStream(fileName));
            while (sr.Peek() > -1)
            {
                str = sr.ReadLine().Split(',');
            }
            sr.Close();
            ClassThick = double.Parse(str[0]);
            V1passH = double.Parse(str[1]);
            V1passC = double.Parse(str[2]);
            Vmpass = double.Parse(str[3]);
            Vfinal = double.Parse(str[4]);
        }
    
    }
}
