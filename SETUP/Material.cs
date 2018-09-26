using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace SETUP
{
    public class Material
    {
        public string CoilNo;
        public double EntryThickness;
        public double ExitThickness;
        public double Width;
        public string SteelGrade;
        public string SteelCode;
        public string SteelSeries;
        public double MaxUnitTension;
        public double Maxthic1, Maxthic2, R1max, Rmean;

        public double[] H2 = new double[20]; //패스별 출측두께 H[1] -> 1Pass출측두께
        public double[] RRA = new double[20]; //패스별 압하율
        public double[] Hexit = new double[20]; //패스별 출측두께
        public double[] RRTA = new double[20]; //패스별 Total 압하율
        public double[] FRIA = new double[20]; //패스별 마찰계수
        public double[] TENA = new double[20]; //패스별 입측단위장력
        public double[] TEXA = new double[20]; //패스별 출측단위장력
        public double[] TTENA = new double[20]; //패스별 Total 입측장력
        public double[] TTEXA = new double[20]; //패스별 Total 출측장력
        public double[] PTOTALA = new double[20]; //패스별 롤포스
        public double[] VA = new double[20]; //패스별 Speed
        public int n; // Total Pass수

        public double[] pp = new double[6];

        public int ReadSteelGrade(ref string[,] ArraySteel)
        {
            string fileName = "SETUP.Resources.Steel.txt";
            Assembly a = Assembly.GetExecutingAssembly();
            string[] str;
            int i = 0;
            StreamReader sr = new StreamReader(a.GetManifestResourceStream(fileName));
            while (sr.Peek() > -1)
            {
                str = sr.ReadLine().Split(',');
                ArraySteel[i, 0] = str[0];//Steel Grade
                ArraySteel[i, 1] = str[1];//Steel Series
                ArraySteel[i, 2] = str[2];//Max unit tension
                i++;
            }
            sr.Close();
            return i;
        }
        public void ReadStress(string item)
        {
            string fileName = "SETUP.Resources.pp.txt";
            Assembly a = Assembly.GetExecutingAssembly();
            string[] str;
            StreamReader sr = new StreamReader(a.GetManifestResourceStream(fileName));
            while (sr.Peek() > -1)
            {
                str = sr.ReadLine().Split(',');
                if (item == str[0])
                {
                    for (int i=0; i<6;i++)
                    {
                        pp[i] = double.Parse(str[i+1]);//변형저항 회귀계수
                    }
                    break;
                }
            }
            sr.Close();
        }
        public void ReadThick2(string series)
        {
            string fileName = "SETUP.Resources.Thick2.txt";
            Assembly a = Assembly.GetExecutingAssembly();
            string[] str;
            StreamReader sr = new StreamReader(a.GetManifestResourceStream(fileName));
            while (sr.Peek() > -1)
            {
                str = sr.ReadLine().Split(',');
                if (series == str[0])
                {
                    Maxthic1 = double.Parse(str[1]);
                    Maxthic2 = double.Parse(str[2]);
                }
            }
            sr.Close();
        }
        public void ReadR1maxRmean(string series, string thickness)
        {
            int max = 0, mean = 0;
            string fileName = "SETUP.Resources.r1maxrmean.txt";
            Assembly a = Assembly.GetExecutingAssembly();
            string[] str;
            StreamReader sr = new StreamReader(a.GetManifestResourceStream(fileName));
            switch (series)
            {
                case "AUS": max = 1; mean = 4; break;
                case "FER": max = 2; mean = 5; break;
                case "MAR": max = 3; mean = 6; break;
            }
            while (sr.Peek() > -1)
            {
                str = sr.ReadLine().Split(',');
                if (double.Parse(thickness) <= double.Parse(str[0]))
                {
                    R1max = double.Parse(str[max]);
                    Rmean = double.Parse(str[mean]);
                }
            }
            sr.Close();
        }
        public void InpuData()
        {
            EntryThickness = double.Parse(SETUP.MainWindow.AppWindow.txtEntryThickness.Text);
            ExitThickness= double.Parse(SETUP.MainWindow.AppWindow.txtExitThickness.Text);
            Width = double.Parse(SETUP.MainWindow.AppWindow.txtWidth.Text);
        }

        public void Schedule()
        {
            double tored = 0, tored2 = 0, alph2 = 0, thic1 = 0, par1 = 0, delta = 0, delta3 = 0, thic3 = 0;
            int ipass = 0; //패스 count
            n = 0;

            for (int i = 0; i <= 19; i++) //Pass별 데이터 초기화
            {
                RRA[i] = 0;
                RRTA[i] = 0;
                FRIA[i] = 0;
                TENA[i] = 0;
                TEXA[i] = 0;
                TTENA[i] = 0;
                TTEXA[i] = 0;
                PTOTALA[i] = 0;
                VA[i] = 0;
            }

            // 1 Pass 출측두께, 2Pass 이후 압하율, Total 압하율 계산
            H2[0] = EntryThickness; // H2[0] 소재두께
            H2[1] = EntryThickness * (100.0 - R1max) / 100.0; // 1Pass 출측 두께 

            thic1 = EntryThickness - H2[1];
            if (thic1 > Maxthic1) H2[1] = EntryThickness - Maxthic1;// 1pass Max 압하량 초과시 1Pass 출측 두께
            tored2 = (H2[1] - ExitThickness) / H2[1]; // 2Pass이후 Total 압하율 계산

            ipass = 0;
            tored = (EntryThickness - ExitThickness) / EntryThickness * 100; // Total 압하율 계산

            ////////////////////////////////////////
            // Pass 수, Pass별 두께 및 압하율 계산  //
            ////////////////////////////////////////

            if (tored < R1max)
            {// 1pass Light Rolling case : Total압하율이 1 Pass Max 압하율 보다 작은 경우
                n = 1;  // 1 Pass
                H2[1] = ExitThickness; // 1 Pass 출측두께에 제품출측두께 입력
                RRA[1] = tored; // 1 Pass 압하율에 Total 압하율 입력
            }
            else
            {// N Pass Rolling case : Total 압하율이 1 Pass Max 압하율 보다 큰 경우
                CalcPassNumber(ref ipass, tored2, ref alph2); // ipass수 계산(2 ~ Final Pass)

                n = ipass + 1; // Total Pass수 계산
                for (int i = 2; i <= n; i++)
                {
                    H2[i] = H2[i - 1] * (100.0 - alph2) / 100.0;

                }
                for (int i = 2; i <= n; i++)
                {
                    if (H2[i - 1] < 4.0) { par1 = 1.05; }  // 압하율 보정
                    else if (H2[i - 1] < 5.0) { par1 = 1.01; }
                    else { par1 = 0.95; }

                    alph2 = alph2 * par1;
                    H2[i] = H2[i - 1] * (100.0 - alph2) / 100.0;

                    delta = H2[i - 1] - H2[i]; //압하량
                    if (delta > Maxthic2) H2[i] = H2[i - 1] - Maxthic2;
                    if (i >= 3)
                    {
                        delta3 = H2[i - 1] - H2[i];
                        thic3 = Maxthic2 - 0.05;
                        if (delta3 > thic3) H2[i] = H2[i - 1] - thic3;
                    }
                    tored2 = (H2[i] - ExitThickness) / H2[i];
                    ipass = ipass - 1;
                    if (ipass == 1) { break; }
                    alph2 = (1.0 - Math.Pow(1.0 - tored2, 1.0 / ipass)) * 100.0;
                }
                H2[n] = ExitThickness; // Final Pass 두께 설정
                RRA[1] = (EntryThickness - H2[1]) / EntryThickness * 100; // 1Pass 압하율 
                for (int i = 2; i <= n; i++)
                {
                    H2[i] = Math.Round(H2[i] * 1000 + 0.5) / 1000; //2 ~ Final Pass 두께 
                    RRA[i] = (H2[i - 1] - H2[i]) / H2[i - 1] * 100; // 2 ~ Final Pass 압하율
                }
            }
            //////////////////////////////////////////////////////////
            // 패스별 Speed, 마찰계수, 롤포스 계산, List Control 적용 //
            //////////////////////////////////////////////////////////
            for (int i = 1; i <= 19; i++)
            {
                double Hen = 0, Hex = 0, v = 0, fri = 0, RR = 0, RRT = 0; // 입측두께, 출측두께, 속도, 마찰계수
                double PTOTAL = 0, Ten = 0, Tex = 0; //롤포스, 입측단위장력, 출측단위장력
                
                Hen = H2[i - 1]; //입측두께
                Hex = H2[i]; //출측두께
                RR = RRA[i]; //압하율
                
                ////Speed 설정
                if (i == 1) v = EntryThickness > SETUP.Mill.AppMill.ClassThick ? SETUP.Mill.AppMill.V1passH : SETUP.Mill.AppMill.V1passC; else v = SETUP.Mill.AppMill.Vmpass; // speed.txt 설정
                if (i == n) v = SETUP.Mill.AppMill.Vfinal;

                ////마찰계수 설정
                //if (v < 300) fri = -0.026 * v / 120.0 + 0.122;
                //else if ((v >= 300.0) & (v < 440.0)) fri = -0.012 * v / 150.0 + 0.0822;
                //else fri = -0.001 * v / 160.0 + 0.04425;
                fri = 0.04;

                Stone(Hen, Hex, RR, i, fri, ref PTOTAL, ref Ten, ref Tex, ref RRT);//롤포스 계산

                //Data Grid Control에 표시할 패스별 출측 두께 계산
                if (i == 1)
                {
                    Hexit[i - 1] = Math.Round(EntryThickness * 1000 + 0.5) / 1000.0;
                    Hexit[i] = Math.Round(Hex * 1000 + 0.5) / 1000.0;
                }
                else
                {
                    Hexit[i] = Math.Round(Hex * 1000 + 0.5) / 1000.0;
                }

                if (i<=n)
                {
                    RRA[i] = Math.Round(RR * 10) / 10;
                    RRTA[i] = Math.Round(RRT * 10) / 10;
                    FRIA[i] = Math.Round(fri * 1000) / 1000;
                    TENA[i] = Math.Round(Ten * 10) / 10;
                    TEXA[i] = Math.Round(Tex * 10) / 10;
                    TTENA[i] = Math.Round(Ten * Hexit[i - 1] * Width * 10) / 10 / 1000;
                    TTEXA[i] = Math.Round(Tex * Hexit[i] * Width * 10) / 10 / 1000;
                    PTOTALA[i] = Math.Round(PTOTAL * 10) / 10;
                    VA[i] = v;
                }
            }
        }

        // Pass Number 계산
        void CalcPassNumber(ref int ipass, double tored2, ref double alph2)
        {
            do
            {
                ipass = ipass + 1;
                alph2 = (1.0 - Math.Pow(1.0 - tored2, 1.0 / ipass)) * 100.0;
            } while (alph2 > Rmean);
        }

        // Roll Force 계산
        void Stone(double Hen, double Hex, double RR, int i, double Fri, ref double PTOTAL, ref double Ten, ref double Tex, ref double RRT)
        {
            const double ElasticModulus = 21000.0, PoissonRatio = 0.27, Pi = 3.141592654;
            double RWR = 0, Red = 0, Hmean = 0, Temp = 0; //Work Roll반경, 압하량, 평균두께, Swap 할 임시 변수
            double Sigma1 = 0, Sigma2 = 0, SigmaMean = 0; //입측, 출측 변형저항, 평균변형저항
            double R1 = 0, R2 = 0; // 입측,출측 압하율
            double Tmean = 0; //입,출측 단위장력, 평균단위장력
            double SeparatedPress = 0, CLNF = 0, X0 = 0, X2 = 0, CLD1 = 0, CLD2 = 0, C1 = 0, C = 0, P0 = 0, PL = 0;
            PTOTAL = 0;
            RWR = SETUP.Mill.AppMill.DiameterOfWorkRoll / 2.0;
            Hex = Hen - RR * Hen / 100.0;
            if (Hex < ExitThickness) Hex = ExitThickness;
            Red = Hen - Hex;
            R1 = (EntryThickness - Hen) / EntryThickness;
            R2 = (EntryThickness - Hex) / EntryThickness;
            RRT = R2 * 100.0;
            Hmean = (Hen + Hex) / 2.0;
            Sigma1 = pp[0] + pp[1] * R1 + pp[2] * Math.Pow(R1, 2.0) + pp[3] * Math.Pow(R1, 3.0) + pp[4] * Math.Pow(R1, 4.0) + pp[5] * Math.Pow(R1, 5.0);
            Sigma2 = pp[0] + pp[1] * R2 + pp[2] * Math.Pow(R2, 2.0) + pp[3] * Math.Pow(R2, 3.0) + pp[4] * Math.Pow(R2, 4.0) + pp[5] * Math.Pow(R2, 5.0);


            Tension(ref Ten, ref Tex, i, Sigma1, Sigma2, Hen, Hex); //장력계산

            Tmean = (Ten + Tex) / 2.0;
            SigmaMean = (Sigma1 + 2.0 * Sigma2) / 3.0;
            SeparatedPress = SigmaMean - Tmean;
            CLNF = Math.Sqrt(RWR * Red);
            X0 = 8.0 * RWR * (1.0 - Math.Pow(PoissonRatio, 2.0)) / (Pi * ElasticModulus) * SeparatedPress;
            X2 = Math.Sqrt(Math.Pow(CLNF, 2.0) + Math.Pow(X0, 2.0));
            CLD1 = X2 + X0;

            do
            {
                C1 = Fri * CLD1 / Hmean;
                C = (Math.Exp(C1) - 1.0) / C1;
                P0 = (SigmaMean - Tmean) * C;
                X0 = 8.0 * RWR * (1.0 - Math.Pow(PoissonRatio, 2.0)) / (Pi * ElasticModulus) * P0;
                X2 = Math.Sqrt(Math.Pow(CLNF, 2.0) + Math.Pow(X0, 2.0));
                CLD2 = X2 + X0;
                Temp = CLD1;
                CLD1 = CLD2;
            } while (Math.Abs(CLD2 - Temp) > 0.05); // CLD1, CLD2 값 비교 차이가 없을때 접촉길이 계산 CLD2

            PL = CLD2; //접촉길이 
            PTOTAL = Width * P0 * PL / 1000.0; //압연하중 계산
        }
        void Tension(ref double Ten, ref double Tex, int i, double Sig1, double Sig2, double Hen, double Hex)
        {
            Ten = 0;
            Tex = 0;
            double Treel1 = 0, Treel2 = 0; //입측장력, 출측장력
            Ten = Sig1 / 2.0;
            Tex = Sig2 / 2.0;
            Treel1 = Hen * Width * Ten;
            Treel2 = Hex * Width * Tex;
            if (i == 1)
            {
                if (Treel1 > SETUP.Mill.AppMill.PORMaxTension) Ten = SETUP.Mill.AppMill.PORMaxTension / (Hen * Width);
                if (Treel2 > SETUP.Mill.AppMill.TRMaxTension) Tex = SETUP.Mill.AppMill.TRMaxTension / (Hex * Width);
                if (Ten > 10) Ten = 10.0;
                if (Tex > 20) Tex = 20.0;
            }
            if (i >= 2)
            {
                if (Treel1 > SETUP.Mill.AppMill.TRMaxTension) Ten = SETUP.Mill.AppMill.TRMaxTension / (Hen * Width);
                if (Treel2 > SETUP.Mill.AppMill.TRMaxTension) Tex = SETUP.Mill.AppMill.TRMaxTension / (Hex * Width);
            }
            if (Ten > MaxUnitTension * 0.9) Ten = MaxUnitTension * 0.9;
            if (Tex > MaxUnitTension) Tex = MaxUnitTension;
            Treel1 = Hen * Width * Ten / 1000.0;
            Treel2 = Hex * Width * Tex / 1000.0;
        }
    }
}
