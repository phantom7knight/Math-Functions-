using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;



namespace mat_290_framework
{
    public partial class MAT290 : Form
    {
        public MAT290()
        {
            InitializeComponent();

            pts_ = new List<Point2D>();
            tVal_ = 0.5F;
            degree_ = 0;
            knot_ = new List<float>();
            EdPtCont_ = true;
            rnd_ = new Random();
        }

        // Point class for general math use
        protected class Point2D : System.Object
        {
            public float x;
            public float y;

            public Point2D(float _x, float _y)
            {
                x = _x;
                y = _y;
            }

            public Point2D(Point2D rhs)
            {
                x = rhs.x;
                y = rhs.y;
            }

            // adds two points together; used for barycentric combos
            public static Point2D operator +(Point2D lhs, Point2D rhs)
            {
                return new Point2D(lhs.x + rhs.x, lhs.y + rhs.y);
            }

            // gets a distance between two points. not actual distance; used for picking
            public static float operator %(Point2D lhs, Point2D rhs)
            {
                float dx = (lhs.x - rhs.x);
                float dy = (lhs.y - rhs.y);

                return (dx * dx + dy * dy);
            }

            // scalar multiplication of points; for barycentric combos
            public static Point2D operator *(float t, Point2D rhs)
            {
                return new Point2D(rhs.x * t, rhs.y * t);
            }

            // scalar multiplication of points; for barycentric combos
            public static Point2D operator *(Point2D rhs, float t)
            {
                return new Point2D(rhs.x * t, rhs.y * t);
            }


            public static Point2D operator *(Point2D rhs, int t)
            {
                return new Point2D(rhs.x * t, rhs.y * t);
            }
            public static Point2D operator *(int t, Point2D rhs)
            {
                return new Point2D(rhs.x * t, rhs.y * t);
            }

            // returns the drawing subsytems' version of a point for drawing.
            public System.Drawing.Point P()
            {
                return new System.Drawing.Point((int)x, (int)y);
            }
        };

        List<Point2D> pts_; // the list of points used in internal algthms
        float tVal_; // t-value used for shell drawing
        int degree_; // degree of deboor subsplines
        List<float> knot_; // knot sequence for deboor
        bool EdPtCont_; // end point continuity flag for std knot seq contruction
        Random rnd_; // random number generator
        bool check_row_echelon_X, check_row_echelon_Y;

        float[,] inputs_array_X;
        float[,] inputs_array_Y;


        float[] output_array_x;
        float[] output_array_y;

        // pickpt returns an index of the closest point to the passed in point
        //  -- usually a mouse position
        private int PickPt(Point2D m)
        {
            float closest = m % pts_[0];
            int closestIndex = 0;

            for (int i = 1; i < pts_.Count; ++i)
            {
                float dist = m % pts_[i];
                if (dist < closest)
                {
                    closest = dist;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private void Menu_Clear_Click(object sender, EventArgs e)
        {
            pts_.Clear();
            Refresh();
        }

        private void Menu_Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MAT290_MouseMove(object sender, MouseEventArgs e)
        {
            // if the right mouse button is being pressed
            if (pts_.Count != 0 && e.Button == MouseButtons.Right)
            {
                // grab the closest point and snap it to the mouse
                int index = PickPt(new Point2D(e.X, e.Y));

                pts_[index].x = e.X;
                pts_[index].y = e.Y;

                Refresh();
            }
        }

        private void MAT290_MouseDown(object sender, MouseEventArgs e)
        {


            //if the left mouse button was clicked
            if (e.Button == MouseButtons.Left)
            {
                //if (pts_.Count >= 3)
                //    return;

                ////dummy points
                //pts_.Add(new Point2D(100, 200));
                //pts_.Add(new Point2D(300, 400));
                //pts_.Add(new Point2D(500, 600));
                //pts_.Add(new Point2D(450, 200));


                // add a new point to the controlPoints
                pts_.Add(new Point2D(e.X, e.Y));
                degree_ = pts_.Count - 1;


                if (Menu_DeBoor.Checked)
                {
                    ResetKnotSeq();
                    UpdateKnotSeq();
                }

                Refresh();
            }

            // if there are points and the middle mouse button was pressed
            if (pts_.Count != 0 && e.Button == MouseButtons.Middle)
            {
                // then delete the closest point
                int index = PickPt(new Point2D(e.X, e.Y));

                pts_.RemoveAt(index);

                if (Menu_DeBoor.Checked)
                {
                    ResetKnotSeq();
                    UpdateKnotSeq();
                }

                Refresh();
            }
        }

        private void MAT290_MouseWheel(object sender, MouseEventArgs e)
        {

            // if the mouse wheel has moved
            if (e.Delta != 0)
            {
                // change the t-value for shell
                tVal_ += e.Delta / 120 * .02f;

                // handle edge cases
                tVal_ = (tVal_ < 0) ? 0 : tVal_;
                tVal_ = (tVal_ > 1) ? 1 : tVal_;

                Refresh();
            }
        }

        private void NUD_degree_ValueChanged(object sender, EventArgs e)
        {
            if (pts_.Count == 0)
                return;

            degree_ = (int)NUD_degree.Value;

            ResetKnotSeq();
            UpdateKnotSeq();

            NUD_degree.Value = degree_;

            Refresh();
        }

        private void CB_cont_CheckedChanged(object sender, EventArgs e)
        {
            EdPtCont_ = CB_cont.Checked;

            ResetKnotSeq();
            UpdateKnotSeq();

            Refresh();
        }

        private void Txt_knot_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' || e.KeyChar == '\n')
            {
                // update knot seq
                string[] splits = Txt_knot.Text.ToString().Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                if (splits.Length > pts_.Count + degree_ + 1)
                    return;

                knot_.Clear();
                foreach (string split in splits)
                {
                    knot_.Add(Convert.ToSingle(split));
                }

                for (int i = knot_.Count; i < (pts_.Count + degree_ + 1); ++i)
                    knot_.Add((float)(i - degree_));

                UpdateKnotSeq();
            }

            Refresh();
        }

        private void Menu_Polyline_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Menu_Points_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Menu_Shell_Click(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Menu_DeCast_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = !Menu_DeCast.Checked;
            Menu_Bern.Checked = false;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = false;
            Menu_Inter_Splines.Checked = false;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = true;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = true;

            ToggleDeBoorHUD(false);

            Refresh();
        }

        private void Menu_Bern_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = false;
            Menu_Bern.Checked = !Menu_Bern.Checked;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = false;
            Menu_Inter_Splines.Checked = false;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = true;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = true;

            ToggleDeBoorHUD(false);

            Refresh();
        }


        private void Menu_Inter_Poly_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = false;
            Menu_Bern.Checked = false;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = !Menu_Inter_Poly.Checked;
            Menu_Inter_Splines.Checked = false;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = true;
            Menu_Polyline.Checked = true;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = false;
            Menu_Shell.Checked = false;

            ToggleDeBoorHUD(false);

            Refresh();
        }

        private void Menu_Inter_Splines_Click(object sender, EventArgs e)
        {
            Menu_DeCast.Checked = false;
            Menu_Bern.Checked = false;
            Menu_Midpoint.Checked = false;

            Menu_Inter_Poly.Checked = false;
            Menu_Inter_Splines.Checked = !Menu_Inter_Splines.Checked;

            Menu_DeBoor.Checked = false;

            Menu_Polyline.Enabled = false;
            Menu_Polyline.Checked = false;
            Menu_Points.Enabled = true;
            Menu_Shell.Enabled = false;
            Menu_Shell.Checked = false;

            ToggleDeBoorHUD(false);

            Refresh();

           
        }
              
        private void DegreeClamp()
        {
            // handle edge cases
            degree_ = (degree_ > pts_.Count - 1) ? pts_.Count - 1 : degree_;
            degree_ = (degree_ < 1) ? 1 : degree_;
        }

        private void ResetKnotSeq()
        {
            DegreeClamp();
            knot_.Clear();

            if (EdPtCont_)
            {
                for (int i = 0; i < degree_; ++i)
                    knot_.Add(0.0f);
                for (int i = 0; i <= (pts_.Count - degree_); ++i)
                    knot_.Add((float)i);
                for (int i = 0; i < degree_; ++i)
                    knot_.Add((float)(pts_.Count - degree_));
            }
            else
            {
                for (int i = -degree_; i <= (pts_.Count); ++i)
                    knot_.Add((float)i);
            }
        }

        private void UpdateKnotSeq()
        {
            Txt_knot.Clear();
            foreach (float knot in knot_)
            {
                Txt_knot.Text += knot.ToString() + " ";
            }
        }

        private void ToggleDeBoorHUD(bool on)
        {
            // set up basic knot sequence
            if (on)
            {
                ResetKnotSeq();
                UpdateKnotSeq();
            }

            CB_cont.Visible = on;

            Lbl_knot.Visible = on;
            Txt_knot.Visible = on;

            Lbl_degree.Visible = on;
            NUD_degree.Visible = on;
        }

        private void MAT290_Paint(object sender, PaintEventArgs e)
        {
            // pass the graphics object to the DrawScreen subroutine for processing
            DrawScreen(e.Graphics);
        }

        private void DrawScreen(System.Drawing.Graphics gfx)
        {
            // to prevent unecessary drawing
            if (pts_.Count == 0)
                return;

            // pens used for drawing elements of the display
            System.Drawing.Pen polyPen = new Pen(Color.Gray, 1.0f);
            System.Drawing.Pen shellPen = new Pen(Color.LightGray, 0.5f);
            System.Drawing.Pen splinePen = new Pen(Color.Black, 1.5f);

            if (Menu_Shell.Checked)
            {
                // draw the shell
                DrawShell(gfx, shellPen, pts_, tVal_);
            }

            if (Menu_Polyline.Checked)
            {
                // draw the control poly
                for (int i = 1; i < pts_.Count; ++i)
                {
                    gfx.DrawLine(polyPen, pts_[i - 1].P(), pts_[i].P());
                }
            }

            if (Menu_Points.Checked)
            {
                // draw the control points
                foreach (Point2D pt in pts_)
                {
                    gfx.DrawEllipse(polyPen, pt.x - 2.0F, pt.y - 2.0F, 4.0F, 4.0F);
                }
            }

            // you can change these variables at will; i have just chosen there
            //  to be six sample points for every point placed on the screen
            float steps = pts_.Count * 6;
            float alpha = 1 / steps;

            ///////////////////////////////////////////////////////////////////////////////
            // Drawing code for algorithms goes in here                                  //
            ///////////////////////////////////////////////////////////////////////////////

            // DeCastlejau algorithm
            if (Menu_DeCast.Checked)
            {
                Point2D current_left;
                Point2D current_right = new Point2D(DeCastlejau(0));

                for (float t = alpha; t < 1; t += alpha)
                {
                    current_left = current_right;
                    current_right = DeCastlejau(t);
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), DeCastlejau(1).P());
            }

            // Bernstein polynomial
            if (Menu_Bern.Checked)
            {
                Point2D current_left;
                Point2D current_right = pts_[0];//new Point2D(Bernstein(0));

                for (float t = alpha; t < 1; t += alpha)
                {
                    current_left = current_right;
                    current_right = Bernstein(t);
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), Bernstein(1).P());
            }


            // Midpoint algorithm
            if (Menu_Midpoint.Checked)
            {
                DrawMidpoint(gfx, splinePen, pts_);
            }




            // polygon interpolation
            if (Menu_Inter_Poly.Checked)
            {
                Point2D current_left;
                Point2D current_right = new Point2D(PolyInterpolate(0));

                for (float t = alpha; t < degree_; t += alpha)
                {
                    current_left = current_right;
                    current_right = PolyInterpolate(t);
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), PolyInterpolate(degree_).P());
            }




            // spline interpolation
            if (Menu_Inter_Splines.Checked)
            {
                

                //preprocessing step
                //-----MATRIX MASALA--------------------------------------------------------------------------------------------------------------------------------------------------------
                inputs_array_X = new float[(pts_.Count + 2), (pts_.Count + 2)];
                inputs_array_Y = new float[(pts_.Count + 2), (pts_.Count + 2)];

                output_array_x = new float[pts_.Count + 2];
                output_array_y = new float[pts_.Count + 2];

               


                //for the outputs
                for (int i = 0; i < pts_.Count + 2; ++i)//
                {
                    if (i < pts_.Count)
                    {

                        output_array_x[i] = pts_[i].x;
                        output_array_y[i] = pts_[i].y;

                    }
                    //setting last values of the output array values to zero
                    if (i == pts_.Count || i == pts_.Count + 1)
                    {
                        output_array_x[i] = 0.0f;
                        output_array_y[i] = 0.0f;
                    }

                    //  Console.WriteLine(output_array_x);

                }

                float k = pts_.Count - 1;


                //check whole of this <===> I  got this bitch

                for (int i = 0; i < pts_.Count + 2; ++i)
                {
                    for (int j = 0; j < pts_.Count + 2; ++j)
                    {

                        //x-value
                        inputs_array_X[i, 0] = 1;
                        inputs_array_X[i, 1] = i;
                        inputs_array_X[i, 2] = i * i;
                        inputs_array_X[i, 3] = i * i * i;


                        if (j > 3)
                        {
                            //x-value
                            inputs_array_X[i, j] = b_truncated_power_calculation(j - 3, i);

                        }


                        if (i == pts_.Count || i == pts_.Count + 1)
                        {
                            //x- value
                            inputs_array_X[i, 0] = 0.0f;
                            inputs_array_X[i, 1] = 0.0f;
                            inputs_array_X[i, 2] = 2.0f;

                            if (i == pts_.Count)
                            {
                                //x-value
                                inputs_array_X[i, 3] = 6 * 0;


                            }
                            else if (i == pts_.Count + 1)
                            {
                                //x-value
                                inputs_array_X[i, 3] = 6 * (pts_.Count - 1);

                            }
                        }
                        //for the double derivatve row1
                        if (i == pts_.Count && j > 3)
                        {
                            //x-value
                            inputs_array_X[i, j] = b_double_derivative_truncated_power_calculation(j - 3, 0);


                        }

                        //for the double derivatve row2
                        if (i == pts_.Count + 1 && j > 3)
                        {
                            //x-value
                            inputs_array_X[i, j] = b_double_derivative_truncated_power_calculation(j - 3, i - 2);

                        }


                        //Console.WriteLine(inputs_array_X[i,j]);
                        //Console.WriteLine("t = " + t);
                    }
                }


                for (int i = 0; i < pts_.Count + 2; ++i)
                {
                    for (int j = 0; j < pts_.Count + 2; ++j)
                    {
                        Console.Write(inputs_array_X[i, j]);
                        inputs_array_Y[i, j] = inputs_array_X[i, j];
                    }
                    Console.WriteLine();
                }


                check_row_echelon_X = ComputeCoefficents(inputs_array_X, output_array_x);
                check_row_echelon_Y = ComputeCoefficents(inputs_array_Y, output_array_y);
                //---------MATRIX MASALA---------------------------------------------------------------------------------------------------------------------------------------------



                Point2D current_left;
                Point2D current_right = new Point2D(SplineInterpolate(0)); //pts_[0];
                for (float t = alpha; t < degree_; t += alpha)
                {
                    current_left = current_right;
                    current_right = SplineInterpolate(t);
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), SplineInterpolate(degree_).P());
            }




            // deboor
            if (Menu_DeBoor.Checked && pts_.Count >= 2)
            {
                Point2D current_left;
                Point2D current_right = new Point2D(DeBoorAlgthm(knot_[degree_]));

                float lastT = knot_[knot_.Count - degree_ - 1] - alpha;
                for (float t = alpha; t < lastT; t += alpha)
                {
                    current_left = current_right;
                    current_right = DeBoorAlgthm(t);
                    gfx.DrawLine(splinePen, current_left.P(), current_right.P());
                }

                gfx.DrawLine(splinePen, current_right.P(), DeBoorAlgthm(lastT).P());
            }

            ///////////////////////////////////////////////////////////////////////////////
            // Drawing code end                                                          //
            ///////////////////////////////////////////////////////////////////////////////


            // Heads up Display drawing code

            Font arial = new Font("Arial", 12);

            if (Menu_DeCast.Checked)
            {
                gfx.DrawString("DeCasteljau", arial, Brushes.Black, 0, 30);
            }
            else if (Menu_Midpoint.Checked)
            {
                gfx.DrawString("Midpoint", arial, Brushes.Black, 0, 30);
            }
            else if (Menu_Bern.Checked)
            {
                gfx.DrawString("Bernstein", arial, Brushes.Black, 0, 30);
            }
            else if (Menu_DeBoor.Checked)
            {
                gfx.DrawString("DeBoor", arial, Brushes.Black, 0, 30);
            }

            gfx.DrawString("t-value: " + tVal_.ToString("F"), arial, Brushes.Black, 500, 30);

            gfx.DrawString("t-step: " + alpha.ToString("F6"), arial, Brushes.Black, 600, 30);

            gfx.DrawString(pts_.Count.ToString(), arial, Brushes.Black, 750, 30);
        }

        //---------------------------------------------------------------------------------------------------

        //Shells calculations


        //---------------------------------------------------------------------------------------------------


        private void DrawShell(System.Drawing.Graphics gfx, System.Drawing.Pen pen, List<Point2D> pts, float t)
        {
            if (Menu_DeCast.Checked)
            {

                int i, j;
                System.Drawing.Pen polyPen = new Pen(Color.Red, 1.0f);
                Brush bru = new SolidBrush(Color.Red);

                //lists initialization 

                List<Point2D> copy = new List<Point2D>();   //1
                List<Point2D> Q = new List<Point2D>();      //2
                List<Point2D> fur = new List<Point2D>();    //3

                //for the calculation of the dashed values
                float[] dashvalues = { 5, 5, 5, 5 };
                Pen dashpen = new Pen(Color.Red, 2);
                dashpen.DashPattern = dashvalues;


                //copy all the original values here
                for (int k = 0; k < pts_.Count; ++k)
                {

                    copy.Add(pts_[k]);

                }

                for (i = degree_; i > 0; --i)
                {

                    for (j = 0; j < i; ++j)
                    {
                        Q.Add(copy[j] * (1 - t) + copy[j + 1] * t);
                        fur.Add(Q[j]);
                    }
                    for (int k = 0; k < i; ++k)
                    {
                        copy[k] = fur[k];
                    }

                    //for (int ab = 1; ab < t; ++ab)
                    for (int ab = 0; ab < Q.Count - 1; ++ab)
                    {

                        gfx.DrawLine(dashpen, Q[ab].P(), Q[ab + 1].P());
                        Console.WriteLine("draw from  {0}" + ab + "to {1}" + ab + 1);
                        gfx.FillEllipse(bru, Q[ab].x, Q[ab].y, 8.0F, 8.0F);
                        gfx.FillEllipse(bru, Q[ab + 1].x, Q[ab + 1].y, 8.0F, 8.0F);


                    }

                    Q.Clear();
                }
                // gfx.DrawEllipse(polyPen, fur[0].x, fur[0].y, 8.0F, 8.0F);

            }
        }

        private Point2D Gamma(int start, int end, float t)
        {
            return new Point2D(0, 0);
        }


        double Multiplication_t(float number_of_times)
        {
            int t;
            double result = 1;

            for (t = 0; t < number_of_times; ++t)
            {
                result = result * (t - number_of_times);
            }


            return result;

        }


        //---------------------------------------------------------------------------------------------------

        //BB form calculations


        //---------------------------------------------------------------------------------------------------
        private Point2D Bernstein(float t)
        {
            //return new Point2D(0, 0);
            Point2D result = new Point2D(0, 0);
            int[,] get_pascal_value = new int[21, 21];
            int i, k;

            for (i = 0; i < degree_ + 1; ++i)//1
            {
                for (k = 0; k <= i; ++k)//i=0
                {
                    if (i == 0 || i == k || k == 0)
                        get_pascal_value[i, k] = 1;
                    else
                    {
                        get_pascal_value[i, k] = get_pascal_value[i - 1, k - 1] + get_pascal_value[i - 1, k];
                    }
                }
            }


            for (int a = 0; a <= degree_; ++a)
            {
                result.x += (float)(pts_[a].x * get_pascal_value[degree_, a] * ((Math.Pow(1 - t, degree_ - a)) * (Math.Pow(t, a))));
                result.y += (float)(pts_[a].y * get_pascal_value[degree_, a] * ((Math.Pow(1 - t, degree_ - a)) * (Math.Pow(t, a))));

            }

            return result;
        }


        //---------------------------------------------------------------------------------------------------

        //DeCastlejau FUCNTIONS


        //---------------------------------------------------------------------------------------------------

        private Point2D DeCastlejau(float t)
        {
            //return new Point2D(0, 0);


            int i, j;
            List<Point2D> Q = new List<Point2D>();
            List<Point2D> copy = new List<Point2D>();

            for (int k = 0; k < degree_ + 1; ++k)
            {

                copy.Add(pts_[k]);

            }

            for (i = degree_; i > 0; --i)
            {

                List<Point2D> fur = new List<Point2D>();
                for (j = 0; j < i; ++j)
                {
                    Q.Add(copy[j] * (1 - t) + copy[j + 1] * t);
                    fur.Add(Q[j]);
                }
                for (int k = 0; k < i; ++k)
                {
                    copy[k] = fur[k];
                }
                Q.Clear();
            }


            return copy[0];

            //Console.WriteLine("{0},{1}", Q[j].x, Q[j].y);


        }

        private const float MAX_DIST = 6.0F;

        //---------------------------------------------------------------------------------------------------

        //MIDPOINT FUCNTIONS


        //---------------------------------------------------------------------------------------------------


        private void DrawMidpoint(System.Drawing.Graphics gfx, System.Drawing.Pen pen, List<Point2D> cPs)
        {

            int i, j;
            float t = 0.5f;
            List<Point2D> Q = new List<Point2D>();
            List<Point2D> copy = new List<Point2D>();

            for (int k = 0; k < degree_ + 1; ++k)
            {

                copy.Add(pts_[k]);

            }
            for (i = degree_; i > 0; --i)
            {

                List<Point2D> fur = new List<Point2D>();
                for (j = 0; j < i; ++j)
                {
                    Q.Add(copy[j] * (1 - t) + copy[j + 1] * t);
                    fur.Add(Q[j]);
                }
                for (int k = 0; k < i; ++k)
                {
                    copy[k] = fur[k];
                }
                Q.Clear();
            }
           
        }

        //---------------------------------------------------------------------------------------------------

        //Polyinterpolate FUCNTIONS


        //---------------------------------------------------------------------------------------------------


        private Point2D PolyInterpolate(float t)
        {
           
            if (pts_.Count < 1)
                return new Point2D(0.0f, 0.0f);
            //Inputs and outputs array decleration
            float[,] inputs_array_X = new float[pts_.Count, (pts_.Count + 2)];
            float[,] inputs_array_Y = new float[pts_.Count, (pts_.Count + 2)];


            int i, j;


            for (i = 0; i < pts_.Count; ++i)
            {
                //X value calculation
                inputs_array_X[i, 0] = i;
                inputs_array_X[i, 1] = pts_[i].x;

                //Y value calculation
                inputs_array_Y[i, 0] = i;
                inputs_array_Y[i, 1] = pts_[i].y;

                //Console.WriteLine(inputs_array_X[i, 0]);


            }


            for (j = 2; j < (pts_.Count + 2); ++j)
            {
                for (i = 0; i < (pts_.Count - j + 1); ++i) //degree_ - 1
                {

                    inputs_array_X[i, j] = (inputs_array_X[(i + 1), (j - 1)] - inputs_array_X[i, (j - 1)]) / (inputs_array_X[(i + j - 1), 0] - inputs_array_X[i, 0]);

                    inputs_array_Y[i, j] = (inputs_array_Y[(i + 1), (j - 1)] - inputs_array_Y[i, (j - 1)]) / (inputs_array_Y[(i + j - 1), 0] - inputs_array_Y[i, 0]);

                }

            }

           
            Point2D result_yolo = new Point2D(0, 0);

            for (i = 0; i < pts_.Count; ++i)
            {
                float ProductX = 1.0f;
                float tempx = inputs_array_X[0, (i + 1)];
                float tempy = inputs_array_Y[0, (i + 1)];

                for (j = 0; j < i; ++j)
                {

                    ProductX = ProductX * (t - inputs_array_X[j, 0]);
                    // ProductY = ProductY * (t - inputs_array_Y[j, 0]);

                }

                // final_result_X[i]
                result_yolo.x += tempx * ProductX;
                result_yolo.y += tempy * ProductX;


            }


            return result_yolo;
        }



        //---------------------------------------------------------------------------------------------------


        //SPLINE FUCNTIONS


        //---------------------------------------------------------------------------------------------------

        //this is for the b values
        float b_truncated_power_calculation(double k, double dt)
        {
            if (dt >= k)
            {
                return (float)Math.Pow((dt - k), 3);
            }
            else
            {
                return 0.0f;
            }
        }

        float b_double_derivative_truncated_power_calculation(double k, double dt)
        {
            if (dt >= k)
            {
                return 6.0f * (float)Math.Pow((dt - k), 1);
            }
            else
            {
                return 0.0f;
            }
        }
        //---------------------------------------------------------------------------------------------------
        //                              CREDIT TO
        //                              MOODIE GADDAR 
        //                              DIGIPEN INSTITUTE OF TECHNOLOGY
        //---------------------------------------------------------------------------------------------------

        bool ComputeCoefficents(float[,] A, float[] B)
        {
            int size = B.Length;
            int sizeMinusOne = size - 1;

            // Row echelon form
            for (int i = 0; i < sizeMinusOne; ++i)
            {
                if (A[i, i] == 0)
                {
                    if (!FindSwappableRow(A, B, i))
                        return false; // No solution
                }

                if (A[i, i] != 1)
                {
                    float coeff = 1 / A[i, i];
                    for (int k = 0; k < size; ++k)
                    {
                        A[i, k] *= coeff;
                    }
                    B[i] *= coeff;
                }

                for (int j = i + 1; j < size; ++j)
                {
                    float coeffeciant = A[j, i] / A[i, i];
                    for (int k = 0; k < size; ++k)
                    {
                        A[j, k] -= coeffeciant * A[i, k];
                    }
                    B[j] -= coeffeciant * B[i];
                }
            }

            if (A[sizeMinusOne, sizeMinusOne] == 0)
                return false; // No solution

            if (A[sizeMinusOne, sizeMinusOne] != 1)
            {
                float coeff = 1 / A[size - 1, size - 1];
                A[sizeMinusOne, sizeMinusOne] *= coeff;
                B[sizeMinusOne] *= coeff;
            }

            // Backward subtitution
            for (int i = sizeMinusOne - 1; i >= 0; --i)
            {
                double coeff = 0;
                for (int j = 0; j < (sizeMinusOne - i); ++j)
                {
                    int k = sizeMinusOne - j;
                    coeff += B[k] * A[i, k];
                }
                B[i] -= (float)coeff;
            }

            return true;
        }
        bool FindSwappableRow(float[,] A, float[] B, int row)
        {
            int start = row + 1;
            double maxValue = 0;
            int bestRowToSwapIndex = row;

            for (int i = start; i < B.Length; ++i)
            {
                if (A[i, row] > maxValue)
                {
                    maxValue = A[i, row];
                    bestRowToSwapIndex = i;
                }
            }

            if (bestRowToSwapIndex == row)
                return false;

            for (int i = 0; i < B.Length; ++i)
            {
                float temp = A[row, i];
                A[row, i] = A[bestRowToSwapIndex, i];
                A[bestRowToSwapIndex, i] = temp;
            }

            float tempB = B[row];
            B[row] = B[bestRowToSwapIndex];
            B[bestRowToSwapIndex] = tempB;

            return true;
        }


      

        //Project 4 splines
        private Point2D SplineInterpolate(float t)
        {
            // return new Point2D(0, 0);

            if (pts_.Count < 1)
                return new Point2D(0.0f, 0.0f);

            //Final Result
            Point2D result = new Point2D(0, 0);

                            

            if (check_row_echelon_X)
            {
                //X{T}
                float temp_a_value = output_array_x[0] + (output_array_x[1] * t) + (output_array_x[2] * t * t) + (output_array_x[3] * t * t * t);
                
                result.x = temp_a_value;
                for (int  i = 4; i < pts_.Count + 2; ++i)
                {
                    result.x += output_array_x[i] * b_truncated_power_calculation(i - 3, t);
                }

                
            }
            else
            {
                Console.WriteLine(" The matrix formed is wrong for X");
            }

            if (check_row_echelon_Y)
            {
                //Y(t)
                float temp_b_value = output_array_y[0] + output_array_y[1] * t + output_array_y[2] * t * t + output_array_y[3] * t * t * t;

                result.y = temp_b_value;

                for ( int i = 4; i < pts_.Count + 2; ++i)
                {
                    result.y += output_array_y[i] * b_truncated_power_calculation(i - 3, t);
                }
            }
            else
            {
                Console.WriteLine(" The matrix formed is wrong for the Y ");
            }


            return result;
        }

        private Point2D DeBoorAlgthm(float t)
        {
            return new Point2D(0, 0);
        }

        private void MAT290_Load(object sender, EventArgs e)
        {

        }

    }
}


