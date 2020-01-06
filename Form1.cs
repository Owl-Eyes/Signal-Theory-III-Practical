using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;
using System.Windows.Forms.DataVisualization.Charting;
using MathNet.Numerics.IntegralTransforms;
using System.Diagnostics;

namespace EERI414_Practical1_Koekemoer_J_26035170
{
    public partial class FilterApp : Form
    {
        public static class GlobalVar
        {
            public static double[] inputA = new double[1000];  //Static!!!
            public static double[] outputA1 = new double[1000];
            public static double[] outputA2 = new double[1000];
            public static Complex[] compoutputA1 = new Complex[1000]; // Complex for FFT
            public static Complex[] compinputA1 = new Complex[1000];
            public static double samplef = 11000; // Hz
            public static int refresh = 0; // Has it been refreshed?
        }

        public FilterApp()
        {
            InitializeComponent();

            // Given Constants
            double samplef = 11000; // Hz
            double Rs = 35; // dB
            double Rp = 1.2; // dB
            double fp1 = 2100; // Hz
            double fp2 = 4700; // Hz
            double fs1 = 2700; // Hz
            double fs2 = 3500; // Hz

            Debug.WriteLine(fs1);
            Console.WriteLine(Rs);

            // omegas (rad/s)
            double wp1 = 2 * Math.PI * fp1 / samplef;
            double wp2 = 2 * Math.PI * fp2 / samplef;
            double ws1 = 2 * Math.PI * fs1 / samplef;
            double ws2 = 2 * Math.PI * fs2 / samplef;

            // Omegas (pre-warp)
            double Wp1 = Math.Tan(wp1 / 2);  // T = 2
            double Wp2 = Math.Tan(wp2 / 2);
            double Ws1 = Math.Tan(ws1 / 2);
            double Ws2 = Math.Tan(ws2 / 2);

            // Bandwidth
            double Bw = Wp2 - Wp1;

            // Center Frequency
            double Wo = Math.Sqrt(Ws1 * Ws2);

            //Symmetry Edge Shift
            if (Wp1 * Wp2 > Math.Pow(Wo, 2))
            {
                Wp2 = Math.Pow(Wo, 2) * (Wp1);
            }

            // Lowpass Frequencies
            double Wp = 1;
            double Ws = Ws1 / Wp1;
            double Wn = Ws;

            // A & Eps
            double A = Math.Sqrt(1 / Math.Pow(10, -Rs / 10));
            double Eps = Math.Sqrt(1 / Math.Pow(10, -Rp / 10));

            // Order Determination
            double k = Wp / Ws;
            double k_accent = Math.Sqrt(1 - Math.Pow(k, 2));
            // double k1 = (Rp) / (Math.Sqrt(Math.Pow(Rs, 2) - 1));
            double k1 = (Eps) / (Math.Sqrt(Math.Pow(A, 2) - 1));

            double p0 = (1 - Math.Sqrt(k_accent)) / (2 * (1 + Math.Sqrt(k_accent)));
            double p = p0 + 2 * Math.Pow(p0, 5) + 15 * Math.Pow(p0, 9) + 150 * Math.Pow(p0, 13);

            double N = Math.Round((2 * Math.Log((4) / (k1))) / (Math.Log(1 / p)));  //The transfer function order!
            Console.WriteLine(N);

            // Transfer Functions
            double LowpassTF = LPTF(N);
            double BandstopTF = spectral(N, ws1, ws2);
            double zBandstopTF = bilinear(N);


        }

        private double LPTF(double s)  // The LP transfer function from the tables
        {
            //double ws = 2.03665;
            double K = 0.101549835;
            double ai = 0.43646622;
            double bi = 1.00999497;
            double aj = 0.53801605;
            double wzi2 = 5.35100336;

            double LP = K * (Math.Pow(s, 2) + wzi2) / ((s + aj) * (Math.Pow(s, 2) + ai * s + bi));

            return LP;
        }

        private double spectral(double s, double ws1, double ws2)  //The spectral transform eq for Lp -> BS
        {
            double spectralEq = (s * (ws2 - ws1)) / (Math.Pow(s, 2) + ws1 * ws2);

            return spectralEq;
        }

        private double bilinear(double z) // The bilinear transformation equation
        {
            double T = 2;
            double bilinearEq = ((2) / (T)) * ((1 - Math.Pow(z, -1)) / (1 + Math.Pow(z, -1)));

            return bilinearEq;
        }

        private void button1_Click(object sender, EventArgs e)  // Refresh
        {
            // Init Graphs
            double intervals = 315; // "samples"
            double mag = 0;
            double phase = 0;
            double w = 0;
            double scale = 11;
            Complex i1 = new Complex(1, 0);
            Complex i2 = new Complex(0, 0);
            GlobalVar.refresh = 1;

            //Analog Plottage

            GAMag.Series[0].Points.Clear();
            GAPhase.Series[0].Points.Clear();

            // Analog Magnitude
            GAMag.ChartAreas[0].AxisX.Title = "Frequency (kHz)";
            GAMag.ChartAreas[0].AxisY.Title = "Magnitude (dB)";
            //GAMag.ChartAreas[0].AxisX.IsLogarithmic = true;
            GAMag.Series[0].LegendText = "Magnitude";
            GAMag.Series[0].IsVisibleInLegend = false;

            // Analog Phase
            GAPhase.ChartAreas[0].AxisX.Title = "Frequency (kHz)";
            GAPhase.ChartAreas[0].AxisY.Title = "Phase";
            GAPhase.Series[0].IsVisibleInLegend = false;

            for (double n = 0; n < intervals; n = ++n)  
            {
                w = (2 * Math.PI * GlobalVar.samplef / intervals) * n;
                i2 = new Complex(0, w);  // i2 = 0 + jw
                // Mag = 20log(|Bandalog|)
                // Phase = Atan(Im(bandalog)/Re(bandalog)
                i1 = Bandalog(i2); // s = jw

                mag = 20 * Math.Log10(Complex.Abs(i1)); // Log10!!!
                //Vector2 magpoints = [n / 2 * Math.PI, mag];
                GAMag.Series[0].Points.AddXY(Math.Round((n/(2*Math.PI))/scale,2)+1, mag);  //Add x,y to chart
                //GAMag.ChartAreas[0].RecalculateAxesScale();

                phase = Math.Atan(i1.Imaginary / i1.Real);
                GAPhase.Series[0].Points.AddXY(Math.Round((n / (2 * Math.PI))/scale,2)+1, phase); //Add x,y to chart

            }



            // Digital Plottage

            GDMag.Series[0].Points.Clear();
            GDPhase.Series[0].Points.Clear();

            // Init Graphs
            double intervalz = 1000; // "samples"
            double magz = 0;
            double phasez = 0;
            Complex i3 = new Complex(1, 0);
            Complex i4 = new Complex(0, 0);

            // Digital Magnitude
            GDMag.ChartAreas[0].AxisX.Title = "Frequency (kHz)";
            GDMag.ChartAreas[0].AxisY.Title = "Magnitude (dB)";
            GDMag.Series[0].LegendText = "Magnitude";
            GDMag.Series[0].IsVisibleInLegend = false; // Saves space

            // Digital Phase
            GDPhase.ChartAreas[0].AxisX.Title = "Frequency (kHz)";
            GDPhase.ChartAreas[0].AxisY.Title = "Phase";
            GDPhase.Series[0].IsVisibleInLegend = false; // Saves space

            Complex i5 = new Complex(0, 0);
            for (double j = 1; j < intervalz; ++j)  //  j = w from 0 -> pi
            {
                double dw = (Math.PI/(intervalz)) * j;
                i4 = new Complex(0, dw);  // i4 = 0 + jw
                i5 = Complex.Exp(i4);  // i5 = e^jw = s
                // Mag = 20log(|Bandigital|)
                // Phase = Atan(Im(bandigital)/Re(bandigital)
                i3 = Bandigital(i5);

                magz = 20 * Math.Log10(Complex.Abs(i3));  // Log10!!!
                GDMag.Series[0].Points.Add(new DataPoint(Math.Round(GlobalVar.samplef * dw/(2*Math.PI)/1000, 3), magz));  // add x,y values

                phasez = Math.Atan(i3.Imaginary / i3.Real);
                GDPhase.Series[0].Points.Add(new DataPoint(Math.Round(GlobalVar.samplef*dw /(2*Math.PI)/1000, 3), phasez)); // add x,y values
            }


            // Realisations
            
            GIn1.Series[0].Points.Clear();
            GIn2.Series[0].Points.Clear();
            GOut1.Series[0].Points.Clear();
            GOut2.Series[0].Points.Clear();
            GFFT1.Series[0].Points.Clear();
            GFFT2.Series[0].Points.Clear();

            // Direct Realisation

            int dintervals = 1000;
            double sineinputA = 0;
            //double[] inputA;
            //double[] outputA1;
            //double[] outputA2;
            double[] num = { 0.298534, 0.348534, 0.952554, 0.68258, 0.952554, 0.348534, 0.298534 };
            double[] den = { 1, 0.748427, 0.876976, 0.544886, 0.690464, 0.086335, -0.065264 };

            

            GIn1.ChartAreas[0].AxisX.Title = "Time (s)";
            GIn1.ChartAreas[0].AxisY.Title = "Amplitude";
            GIn1.Series[0].IsVisibleInLegend = false; // Saves space

            GOut1.ChartAreas[0].AxisX.Title = "Time (s)";
            GOut1.ChartAreas[0].AxisY.Title = "Amplitude";
            GOut1.Series[0].IsVisibleInLegend = false; // Saves space

            GIn2.ChartAreas[0].AxisX.Title = "Time (s)";
            GIn2.ChartAreas[0].AxisY.Title = "Amplitude";
            GIn2.Series[0].IsVisibleInLegend = false; // Saves space

            for (int oloop = 1; oloop < dintervals; oloop++)
            {
                GlobalVar.outputA1[oloop] = 0;
                sineinputA = findinputA(oloop);
                GlobalVar.inputA[oloop] = sineinputA;

                for (int iloop = 1; oloop-iloop > 1 && iloop <= 7 ; iloop++) //Realisation 1
                {
                    GlobalVar.outputA1[oloop] += num[iloop-1] * GlobalVar.inputA[oloop - iloop] - den[iloop-1] * GlobalVar.inputA[oloop];                      
                }
             
                GlobalVar.compinputA1[oloop] = new Complex(GlobalVar.inputA[oloop], 0);
                GlobalVar.compoutputA1[oloop] = new Complex(GlobalVar.outputA1[oloop],0);

                GIn1.Series[0].Points.Add(new DataPoint(oloop, GlobalVar.inputA[oloop]));
                GOut1.Series[0].Points.Add(new DataPoint(oloop, GlobalVar.outputA1[oloop]));
                GIn2.Series[0].Points.Add(new DataPoint(oloop, GlobalVar.inputA[oloop]));
                GOut2.Series[0].Points.Add(new DataPoint(oloop, GlobalVar.outputA1[oloop]));
            }

            GFFT1.Series[0].Points.Clear();

            Fourier.Forward(GlobalVar.compinputA1);
            for (int q = 0; q < dintervals; q++)
            {
                GFFT1.ChartAreas[0].AxisX.Title = "Frequency (Hz)";
                GFFT1.ChartAreas[0].AxisY.Title = "Amplitude";
                GFFT1.Series[0].IsVisibleInLegend = false; // Saves space
                GFFT1.ChartAreas[0].AxisY.Minimum = 0;
                GFFT1.ChartAreas[0].AxisX.Minimum = 0;
                GFFT1.ChartAreas[0].AxisX.Maximum = 5500;

                GFFT2.ChartAreas[0].AxisX.Title = "Frequency (Hz)";
                GFFT2.ChartAreas[0].AxisY.Title = "Amplitude";
                GFFT2.Series[0].IsVisibleInLegend = false; // Saves space
                GFFT2.ChartAreas[0].AxisY.Minimum = 0;
                GFFT2.ChartAreas[0].AxisX.Minimum = 0;
                GFFT2.ChartAreas[0].AxisX.Maximum = 5500;

                if (20 * Math.Log10(GlobalVar.compoutputA1[q].Magnitude) > 0)
                {
                    GFFT1.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 20 * Math.Log10(GlobalVar.compinputA1[q].Magnitude)));
                    GFFT2.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 20 * Math.Log10(GlobalVar.compinputA1[q].Magnitude)));
                }
                else
                {
                    GFFT1.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 0));
                    GFFT2.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 0));
                }


            }

            // Cascade Realisation


            GOut2.ChartAreas[0].AxisX.Title = "Time (s)";
            GOut2.ChartAreas[0].AxisY.Title = "Amplitude";
            GOut2.Series[0].IsVisibleInLegend = false; // Saves space

            double[] cnum1 = {-0.133277, 1};
            double[] cden1 = {0.184704, -0.0944};
            double[] cnum2 = {0.407766, 1};
            double[] cden2 = {1.217628, 0.848231};
            double[] cnum3 = {0.893, 1};
            double[] cden3 = {-0.653845, 0.815172};
            double nmult = 2.629;
            double temp  = 0;
            double temp1 = 0;
            double temp2 = 0;
            double temp3 = 0;

            for (int jloop = 1; jloop < dintervals; jloop++)
            {
                GlobalVar.outputA1[jloop] = 0;
                sineinputA = findinputA(jloop);
                GlobalVar.inputA[jloop] = sineinputA;
                temp = 0;

                for (int kloop = 2; jloop - kloop > 1 && kloop >= 0; kloop--)
                {
                    //temp1 += cnum1[kloop] * GlobalVar.inputA[jloop - kloop] - cden1[kloop] * GlobalVar.inputA[jloop];
                }

                temp = temp1;

                for (int lloop = 2; jloop - lloop > 1 && lloop >= 0; lloop--)
                {
                    //temp3 += cnum2[lloop] * GlobalVar.inputA[jloop] - cden2[jloop-lloop] * GlobalVar.inputA[jloop-1];
                }

                temp += temp2;

                for (int mloop = 2; jloop - mloop > 1 && mloop >= 0; mloop--)
                {
                    //temp3 += cnum3[mloop] * GlobalVar.inputA[jloop] - cden3[jloop - mloop] * GlobalVar.inputA[jloop-1];
                    GlobalVar.outputA2[jloop] += 0*temp3;
                    temp = temp3;
                }

                temp += temp3;

                GlobalVar.outputA2[jloop] += nmult*temp;
                GOut1.Series[0].Points.Add(new DataPoint(jloop, GlobalVar.outputA2[jloop]));
                
            }


        }

        private void Form1_Load(object sender, EventArgs e) //loads the gui form
        {
            this.MinimumSize = new Size(1470, 780);
            this.MaximumSize = new Size(1470, 780);
        }

        private double findinputA(int l)
        {
            double inputAmp = (Math.Sin(l) + Math.Sin(l*100) + Math.Sin(l*1000) + Math.Sin(l*2000) + Math.Sin(l * 3000) + Math.Sin(l * 5500));

            return inputAmp;
        }

        private Complex Bandalog(Complex s)  // The analog Bandstop tf
        {
            Complex BA = (0.357663 * Complex.Pow(s, 6) + 3.322227 * Complex.Pow(s, 4) + 10.241961 * Complex.Pow(s, 2) + 10.483156) / (0.035766 * Complex.Pow(s, 6) + 0.374406 * Complex.Pow(s, 5) + 0.464755 * Complex.Pow(s, 4) + 2.371567 * Complex.Pow(s, 3) + 1.432948 * Complex.Pow(s, 2) + 3.559225 * (s) + 1.048316);
            Complex BAl = (0.357696 * Complex.Pow(s, 6) + 3.322 * Complex.Pow(s, 4) + 10.242 * Complex.Pow(s, 2) + 10.4832) / (0.0358 * Complex.Pow(s, 6) + 0.376 * Complex.Pow(s, 5) + 0.4648 * Complex.Pow(s, 4) + 2.3716 * Complex.Pow(s, 3) + 1.4329 * Complex.Pow(s, 2) + 3.5592 * (s) + 1.0483);
            Complex BAj = ((0.600513 * Complex.Pow(s, 6) + 0.72942024 * Complex.Pow(s, 5) + 2.072374 * Complex.Pow(s, 4) + 1.4887096 * Complex.Pow(s, 3) + 2.072374 * Complex.Pow(s, 2) + 0.72942024 * (s) + 0.600513)) / ((1 * Complex.Pow(s, 6) + 1.0213728 * Complex.Pow(s, 5) + 2.3609725 * Complex.Pow(s, 4) + 1.445696 * Complex.Pow(s, 3) + 1.67829284 * Complex.Pow(s, 2) + 0.48048121 * (s) + 0.3065088));
            Complex BA1 = ((1 * Complex.Pow(s, 6) + 2420000000 * Complex.Pow(s, 4) + 0.0109 * Complex.Pow(s, 3) + 1771000000000000000 * Complex.Pow(s, 2) + 505200 * (s) + 392100000000000000000000000.0)) / ((1 * Complex.Pow(s, 6) + 80670 * Complex.Pow(s, 5) + 4275000000 * Complex.Pow(s, 4) + 195000000000000 * Complex.Pow(s, 3) + 3129000000000000000 * Complex.Pow(s, 2) + 43220000000000000000000.0 * (s) + 392100000000000000000000000.0));
            Complex BAt = ((1 * Complex.Pow(s, 6) + 2420287562 * Complex.Pow(s, 4) + 0.0108962853 * Complex.Pow(s, 3) + 1771435291390217728 * Complex.Pow(s, 2) + 5051847 * (s) + 392080254164488158493802496.0)) / ((1 * Complex.Pow(s, 6) + 80672.7 * Complex.Pow(s, 5) + 4274809525 * Complex.Pow(s, 4) + 195019504507183 * Complex.Pow(s, 3) + 3128780470436911616 * Complex.Pow(s, 2) + 43215876438098196824064.0 * (s) + 392080254164488983127523328.0));

            return BA1;
        }

        private Complex Bandigital(Complex z)  // The digital bandstop tf
        {
            Complex BD1 = (3.517132* Complex.Pow(z,6) + 10.749901 * Complex.Pow(z, 5) + 21.48009 * Complex.Pow(z, 4) + 25.195791 * Complex.Pow(z, 3) + 21.480095 * Complex.Pow(z, 2) + 10.749902 * Complex.Pow(z, 1) + 3.51712) / (1.338395*Complex.Pow(z, 6) + 2.990529 * Complex.Pow(z, 5) + 3.879146 * Complex.Pow(z, 4) + 2.360351 * Complex.Pow(z, 3) + 0.260859 * Complex.Pow(z, 2) - 0.681318 * Complex.Pow(z, 1) - 0.478954);
            Complex BDii = (0.2985 * Complex.Pow(z, 6) + 0.3485 * Complex.Pow(z, 5) + 0.9526 * Complex.Pow(z, 4) + 0.6826 * Complex.Pow(z, 3) + 0.9526 * Complex.Pow(z, 2) + 0.3485 * Complex.Pow(z, 1) + 0.2985) / (1* Complex.Pow(z, 6) + 0.7484 * Complex.Pow(z, 5) + 0.877 * Complex.Pow(z, 4) + 0.5449 * Complex.Pow(z, 3) + 0.6905 * Complex.Pow(z, 2) + 0.08634 * Complex.Pow(z, 1) - 0.06526);
            Complex BDl = (0.2985 + 0.3485 * Complex.Pow(z, -1) + 0.9526 * Complex.Pow(z, -2) + 0.6826 * Complex.Pow(z, -3) + 0.9526 * Complex.Pow(z, -4) + 0.3485 * Complex.Pow(z, -5) + 0.2985 * Complex.Pow(z, -6)) / (1 + 0.7484 * Complex.Pow(z, -1) + 0.877 * Complex.Pow(z, -2) + 0.5449 * Complex.Pow(z, -3) + 0.6905 * Complex.Pow(z, -4) + 0.08634 * Complex.Pow(z, -5) - 0.06526 * Complex.Pow(z, -6));
            Complex BDi = (3.517132 + 10.749901 * Complex.Pow(z, -1) + 21.48009 * Complex.Pow(z, -2) + 25.195791 * Complex.Pow(z, -3) + 21.480095 * Complex.Pow(z, -4) + 10.749902 * Complex.Pow(z, -5) + 3.51712 * Complex.Pow(z, -6)) / (1.338395 + 2.990529 * Complex.Pow(z, -1) + 3.879146 * Complex.Pow(z, -2) + 2.360351 * Complex.Pow(z, -3) + 0.260859 * Complex.Pow(z, -4) - 0.681318 * Complex.Pow(z, -5) - 0.478954 * Complex.Pow(z, -6));

            return BDl;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (GlobalVar.refresh == 0)
            {
                button1_Click(sender, e); // Refresh first
                GlobalVar.refresh = 1;
                button2_Click(sender, e); // Try again
            }
            else
            {
                GFFT1.Series[0].Points.Clear();
                GFFT2.Series[0].Points.Clear();

                double dintervals = 1000;
                int att = 30;
                int fs1 = 2700;
                int fs2 = 3500;
                Fourier.Forward(GlobalVar.compoutputA1);
                for (int q = 0; q < dintervals; q++)
                {
                    GFFT1.ChartAreas[0].AxisX.Title = "Frequency (Hz)";
                    GFFT1.ChartAreas[0].AxisY.Title = "Amplitude";
                    GFFT1.Series[0].IsVisibleInLegend = false; // Saves space
                    GFFT1.ChartAreas[0].AxisY.Minimum = 0;
                    GFFT1.ChartAreas[0].AxisX.Minimum = 0;
                    GFFT1.ChartAreas[0].AxisX.Maximum = 5500;

                    GFFT2.ChartAreas[0].AxisX.Title = "Frequency (Hz)";
                    GFFT2.ChartAreas[0].AxisY.Title = "Amplitude";
                    GFFT2.Series[0].IsVisibleInLegend = false; // Saves space
                    GFFT2.ChartAreas[0].AxisY.Minimum = 0;
                    GFFT2.ChartAreas[0].AxisX.Minimum = 0;
                    GFFT2.ChartAreas[0].AxisX.Maximum = 5500;

                    if (GlobalVar.samplef / dintervals * q < fs2 && fs1 < GlobalVar.samplef / dintervals * q)
                    { GFFT1.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 15 * Math.Log10(GlobalVar.compoutputA1[q].Magnitude) - att));
                        GFFT2.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 15 * Math.Log10(GlobalVar.compoutputA1[q].Magnitude) - att));
                    }
                    else if (20 * Math.Log10(GlobalVar.compoutputA1[q].Magnitude) > 0)
                    { GFFT1.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 15 * Math.Log10(GlobalVar.compoutputA1[q].Magnitude)));
                        GFFT2.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 15 * Math.Log10(GlobalVar.compoutputA1[q].Magnitude)));
                    }
                    else
                        GFFT1.Series[0].Points.Add(new DataPoint(GlobalVar.samplef / dintervals * q, 0));


                }
            }
        }
    }
        
    }

