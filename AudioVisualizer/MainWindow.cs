using LibAudioVisualizer;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Drawing.Drawing2D;
using System.Numerics;

namespace AudioVisualizer
{
    public partial class MainWindow : Form
    {
        WasapiCapture capture;             // ��Ƶ����
        Visualizer visualizer;             // ���ӻ�
        double[]? spectrumData;            // Ƶ������

        Color[] allColors;                 // ������ɫ

        public MainWindow()
        {
            capture = new WasapiLoopbackCapture();          // ������Է���������
            visualizer = new Visualizer(256);               // �½�һ�����ӻ���, ��ʹ�� 256 ���������и���Ҷ�任

            allColors = GetAllHsvColors();                  // ��ȡ���еĽ�����ɫ (HSV ��ɫ)

            capture.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(8192, 1);      // ָ������ĸ�ʽ, ������, 32λ���, IeeeFloat ����, 8192������
            capture.DataAvailable += Capture_DataAvailable;                          // �����¼�

            InitializeComponent();
        }

        /// <summary>
        /// ��ȡ HSV �����еĻ�����ɫ (���ͶȺ����Ⱦ�Ϊ���ֵ)
        /// </summary>
        /// <returns>���е� HSV ������ɫ(�� 256 * 6 ��, ����������������, ��ɫҲ�ὥ��)</returns>
        private Color[] GetAllHsvColors()
        {
            Color[] result = new Color[256 * 6];

            for (int i = 0; i < 256; i++)
            {
                result[i] = Color.FromArgb(255, i, 0);
            }

            for (int i = 0; i < 256; i++)
            {
                result[256 + i] = Color.FromArgb(255 - i, 255, 0);
            }

            for (int i = 0; i < 256; i++)
            {
                result[512 + i] = Color.FromArgb(0, 255, i);
            }

            for (int i = 0; i < 256; i++)
            {
                result[768 + i] = Color.FromArgb(0, 255 - i, 255);
            }

            for (int i = 0; i < 256; i++)
            {
                result[1024 + i] = Color.FromArgb(i, 0, 255);
            }

            for (int i = 0; i < 256; i++)
            {
                result[1280 + i] = Color.FromArgb(255, 0, 255 - i);
            }

            return result;
        }

        /// <summary>
        /// �����������ݵ�ʱ��, �������ӻ�������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            int length = e.BytesRecorded / 4;           // ���������� (ÿһ�������� 4 �ֽ�)
            double[] result = new double[length];       // �������

            for (int i = 0; i < length; i++)
                result[i] = BitConverter.ToSingle(e.Buffer, i * 4);      // ȡ������ֵ

            visualizer.PushSampleData(result);          // ���µĲ����洢�� ���ӻ��� ��
        }

        /// <summary>
        /// ����ˢ��Ƶ�������Լ�ʵ��Ƶ�����ݻ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataTimer_Tick(object? sender, EventArgs e)
        {
            double[] newSpectrumData = visualizer.GetSpectrumData();         // �ӿ��ӻ����л�ȡƵ������
            newSpectrumData = Visualizer.GetBlurry(newSpectrumData, 2);                // ƽ��Ƶ������

            spectrumData = newSpectrumData;
        }

        /// <summary>
        /// ����һ������� ����
        /// </summary>
        /// <param name="g">��ͼĿ��</param>
        /// <param name="down">�·���ɫ</param>
        /// <param name="up">�Ϸ���ɫ</param>
        /// <param name="spectrumData">Ƶ������</param>
        /// <param name="pointCount">������, �������</param>
        /// <param name="drawingWidth">���˵Ŀ��</param>
        /// <param name="xOffset">���˵���ʼX����</param>
        /// <param name="yOffset">���˵���ʵY����</param>
        /// <param name="scale">Ƶ�׵�����(ʹ�ø�ֵ���Է�ת����)</param>
        private void DrawGradient(Graphics g, Color down, Color up, double[] spectrumData, int pointCount, int drawingWidth, float xOffset, float yOffset, double scale)
        {
            GraphicsPath path = new GraphicsPath();

            PointF[] points = new PointF[pointCount + 2];
            for (int i = 0; i < pointCount; i++)
            {
                double x = i * drawingWidth / pointCount + xOffset;
                double y = spectrumData[i * spectrumData.Length / pointCount] * scale + yOffset;
                points[i + 1] = new PointF((float)x, (float)y);
            }

            points[0] = new PointF(xOffset, yOffset);
            points[points.Length - 1] = new PointF(xOffset + drawingWidth, yOffset);

            path.AddCurve(points);

            float upP = (float)points.Min(v => v.Y);

            if (Math.Abs(upP - yOffset) < 1)
                return;

            using Brush brush = new LinearGradientBrush(new PointF(0, yOffset), new PointF(0, upP), down, up);
            g.FillPath(brush, path);
        }

        /// <summary>
        /// ���ƽ��������
        /// </summary>
        /// <param name="g">��ͼĿ��</param>
        /// <param name="down">�·���ɫ</param>
        /// <param name="up">�Ϸ���ɫ</param>
        /// <param name="spectrumData">Ƶ������</param>
        /// <param name="stripCount">���ε�����</param>
        /// <param name="drawingWidth">��ͼ�Ŀ��</param>
        /// <param name="xOffset">��ͼ����ʼ X ����</param>
        /// <param name="yOffset">��ͼ����ʼ Y ����</param>
        /// <param name="spacing">����������֮��ļ��(����)</param>
        /// <param name="scale"></param>
        private void DrawGradientStrips(Graphics g, Color down, Color up, double[] spectrumData, int stripCount, int drawingWidth, float xOffset, float yOffset, float spacing, double scale)
        {
            float stripWidth = (drawingWidth - spacing * stripCount) / stripCount;
            PointF[] points = new PointF[stripCount];

            for (int i = 0; i < stripCount; i++)
            {
                double x = stripWidth * i + spacing * i + xOffset;
                double y = spectrumData[i * spectrumData.Length / stripCount] * scale;   // height
                points[i] = new PointF((float)x, (float)y);
            }

            float upP = (float)points.Min(v => v.Y < 0 ? yOffset + v.Y : yOffset);
            float downP = (float)points.Max(v => v.Y < 0 ? yOffset : yOffset + v.Y);

            if (downP < yOffset)
                downP = yOffset;

            if (Math.Abs(upP - downP) < 1)
                return;

            using Brush brush = new LinearGradientBrush(new PointF(0, downP), new PointF(0, upP), down, up);

            for (int i = 0; i < stripCount; i++)
            {
                PointF p = points[i];
                float y = yOffset;
                float height = p.Y;

                if (height < 0)
                {
                    y += height;
                    height = -height;
                }

                g.FillRectangle(brush, new RectangleF(p.X, y, stripWidth, height));
            }
        }

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pen"></param>
        /// <param name="spectrumData"></param>
        /// <param name="pointCount"></param>
        /// <param name="drawingWidth"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="scale"></param>
        private void DrawCurve(Graphics g, Pen pen, double[] spectrumData, int pointCount, int drawingWidth, double xOffset, double yOffset, double scale)
        {
            PointF[] points = new PointF[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                double x = i * drawingWidth / pointCount + xOffset;
                double y = spectrumData[i * spectrumData.Length / pointCount] * scale + yOffset;
                points[i] = new PointF((float)x, (float)y);
            }

            g.DrawCurve(pen, points);
        }

        /// <summary>
        /// ���򵥵�Բ������
        /// </summary>
        /// <param name="g"></param>
        /// <param name="brush"></param>
        /// <param name="spectrumData"></param>
        /// <param name="stripCount"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="radius"></param>
        /// <param name="spacing"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        private void DrawCircleStrips(Graphics g, Brush brush, double[] spectrumData, int stripCount, double xOffset, double yOffset, double radius, double spacing, double rotation, double scale)
        {
            double rotationAngle = Math.PI / 180 * rotation;
            double blockWidth = MathF.PI * 2 / stripCount;           // angle
            double stripWidth = blockWidth - MathF.PI / 180 * spacing;                // angle
            PointF[] points = new PointF[stripCount];

            for (int i = 0; i < stripCount; i++)
            {
                double x = blockWidth * i + rotationAngle;      // angle
                double y = spectrumData[i * spectrumData.Length / stripCount] * scale;   // height
                points[i] = new PointF((float)x, (float)y);
            }

            for (int i = 0; i < stripCount; i++)
            {
                PointF p = points[i];
                double sinStart = Math.Sin(p.X);
                double sinEnd = Math.Sin(p.X + stripWidth);
                double cosStart = Math.Cos(p.X);
                double cosEnd = Math.Cos(p.X + stripWidth);

                PointF[] polygon = new PointF[]
                {
                    new PointF((float)(cosStart * radius + xOffset), (float)(sinStart * radius + yOffset)),
                    new PointF((float)(cosEnd * radius + xOffset), (float)(sinEnd * radius + yOffset)),
                    new PointF((float)(cosEnd * (radius + p.Y) + xOffset), (float)(sinEnd * (radius + p.Y) + yOffset)),
                    new PointF((float)(cosStart * (radius + p.Y) + xOffset), (float)(sinStart * (radius + p.Y) + yOffset)),
                };

                g.FillPolygon(brush, polygon);
            }
        }

        /// <summary>
        /// ��Բ��������
        /// </summary>
        /// <param name="g"></param>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <param name="spectrumData"></param>
        /// <param name="stripCount"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="radius"></param>
        /// <param name="spacing"></param>
        /// <param name="scale"></param>
        private void DrawCircleGradientStrips(Graphics g, Color inner, Color outer, double[] spectrumData, int stripCount, double xOffset, double yOffset, double radius, double spacing, double rotation, double scale)
        {
            double rotationAngle = Math.PI / 180 * rotation;
            double blockWidth = Math.PI * 2 / stripCount;           // angle
            double stripWidth = blockWidth - MathF.PI / 180 * spacing;                // angle
            PointF[] points = new PointF[stripCount];

            for (int i = 0; i < stripCount; i++)
            {
                double x = blockWidth * i + rotationAngle;      // angle
                double y = spectrumData[i * spectrumData.Length / stripCount] * scale;   // height
                points[i] = new PointF((float)x, (float)y);
            }

            double maxHeight = points.Max(v =>  v.Y);
            double outerRadius = radius + maxHeight;

            PointF[] polygon = new PointF[4];
            for (int i = 0; i < stripCount; i++)
            {
                PointF p = points[i];
                double sinStart = Math.Sin(p.X);
                double sinEnd = Math.Sin(p.X + stripWidth);
                double cosStart = Math.Cos(p.X);
                double cosEnd = Math.Cos(p.X + stripWidth);

                PointF
                    p1 = new PointF((float)(cosStart * radius + xOffset),(float)(sinStart * radius + yOffset)),
                    p2 = new PointF((float)(cosEnd * radius + xOffset),(float)(sinEnd * radius + yOffset)),
                    p3 = new PointF((float)(cosEnd * (radius + p.Y) + xOffset), (float)(sinEnd * (radius + p.Y) + yOffset)),
                    p4 = new PointF((float)(cosStart * (radius + p.Y) + xOffset), (float)(sinStart * (radius + p.Y) + yOffset));

                polygon[0] = p1;
                polygon[1] = p2;
                polygon[2] = p3;
                polygon[3] = p4;


                PointF innerP = new PointF((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
                PointF outerP = new PointF((p3.X + p4.X) / 2, (p3.Y + p4.Y) / 2);

                Vector2 offset = new Vector2(outerP.X - innerP.X, outerP.Y - innerP.Y);
                if (MathF.Sqrt(offset.X * offset.X + offset.Y * offset.Y) < 1)                                // �����ˢ����֮����벻��̫С
                    continue;

                try
                {
                    using LinearGradientBrush brush = new LinearGradientBrush(innerP, outerP, inner, outer);        // ��������ѧ bug, ��� ���Ա�ˢ�� OutMemoryException
                    g.FillPolygon(brush, polygon);                                                            // ����ʵ���ϲ�Ӧ��������쳣...
                }
                catch { }
            }
        }

        /// <summary>
        /// ���򵥵�����
        /// </summary>
        /// <param name="g"></param>
        /// <param name="brush"></param>
        /// <param name="spectrumData"></param>
        /// <param name="stripCount"></param>
        /// <param name="drawingWidth"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="spacing"></param>
        /// <param name="scale"></param>
        private void DrawStrips(Graphics g, Brush brush, double[] spectrumData, int stripCount, int drawingWidth, float xOffset, float yOffset, float spacing, double scale)
        {
            float stripWidth = (drawingWidth - spacing * stripCount) / stripCount;
            PointF[] points = new PointF[stripCount];

            for (int i = 0; i < stripCount; i++)
            {
                double x = stripWidth * i + spacing * i + xOffset;
                double y = spectrumData[i * spectrumData.Length / stripCount] * scale;   // height
                points[i] = new PointF((float)x, (float)y);
            }

            for (int i = 0; i < stripCount; i++)
            {
                PointF p = points[i];
                float y = yOffset;
                float height = p.Y;

                if (height < 0)
                {
                    y += height;
                    height = -height;
                }

                g.FillRectangle(brush, new RectangleF(p.X, y, stripWidth, height));
            }
        }

        /// <summary>
        /// ������ı߿�
        /// </summary>
        /// <param name="g"></param>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <param name="area"></param>
        /// <param name="scale"></param>
        /// <param name="width"></param>
        private void DrawGradientBorder(Graphics g, Color inner, Color outer, Rectangle area, double scale, float width)
        {
            int thickness = (int)(width * scale);
            if (thickness < 1)
                return;

            Rectangle rect = new Rectangle(area.X, area.Y, area.Width, area.Height);

            Rectangle up = new Rectangle(rect.Location, new Size(rect.Width, thickness));
            Rectangle down = new Rectangle(new Point(rect.X, (int)(rect.X + rect.Height - scale * width)), new Size(rect.Width, thickness));
            Rectangle left = new Rectangle(rect.Location, new Size(thickness, rect.Height));
            Rectangle right = new Rectangle(new Point((int)(rect.X + rect.Width - scale * width), rect.Y), new Size(thickness, rect.Height));

            LinearGradientBrush upB = new LinearGradientBrush(up, outer, inner, LinearGradientMode.Vertical);
            LinearGradientBrush downB = new LinearGradientBrush(down, inner, outer, LinearGradientMode.Vertical);
            LinearGradientBrush leftB = new LinearGradientBrush(left, outer, inner, LinearGradientMode.Horizontal);
            LinearGradientBrush rightB = new LinearGradientBrush(right, inner, outer, LinearGradientMode.Horizontal);

            upB.WrapMode = downB.WrapMode = leftB.WrapMode = rightB.WrapMode = WrapMode.TileFlipXY;

            g.FillRectangle(upB, up);
            g.FillRectangle(downB, down);
            g.FillRectangle(leftB, left);
            g.FillRectangle(rightB, right);
        }

        int colorIndex = 0;
        double rotation = 0;
        BufferedGraphics? oldBuffer;
        private void DrawingTimer_Tick(object? sender, EventArgs e)
        {
            if (spectrumData == null)
                return;

            rotation += .1;
            colorIndex++;

            Color color1 = allColors[colorIndex % allColors.Length];
            Color color2 = allColors[(colorIndex + 200) % allColors.Length];

            double[] bassArea = Visualizer.TakeSpectrumOfFrequency(spectrumData, capture.WaveFormat.SampleRate, 250);       // ��Ƶ����
            double bassScale = bassArea.Average() * 100;                                                                    // �������µ����� (������)
            double extraScale = Math.Min(drawingPanel.Width, drawingPanel.Height) / 6;                                      // �������µ����� (���ϴ��ڴ�С)

            Rectangle border = new Rectangle(Point.Empty, drawingPanel.Size);

            BufferedGraphics buffer = BufferedGraphicsManager.Current.Allocate(drawingPanel.CreateGraphics(), drawingPanel.ClientRectangle);
            Graphics g = buffer.Graphics;

            if (oldBuffer != null)
            {
                //oldBuffer.Render(buffer.Graphics);      // �������Ҫʵ�� "��Ӱ" Ч��, ��ȡ��ע����δ���, ���ҽ� g.Clear ��Ϊ g.FillRectange(xxx, ��͸���ĺ�ɫ)
                oldBuffer.Dispose();
            }

            using Pen pen = new Pen(Color.Pink);                  // ����Ƶ���������õı�

            g.SmoothingMode = SmoothingMode.HighQuality;          // ���˺�, �Ǳ�����Ǹ�������ͼ
            g.Clear(drawingPanel.BackColor);

            DrawGradientBorder(g, Color.FromArgb(0, color1), color2, border, bassScale, drawingPanel.Width / 10);
            DrawGradientStrips(g, color1, color2, spectrumData, spectrumData.Length, drawingPanel.Width, 0, drawingPanel.Height, 3, -drawingPanel.Height * 10);
            DrawCircleGradientStrips(g, color1, color2, spectrumData, spectrumData.Length, drawingPanel.Width / 2, drawingPanel.Height / 2, MathF.Min(drawingPanel.Width, drawingPanel.Height) / 4 + extraScale * bassScale, 1, rotation, drawingPanel.Width / 6 * 10);

            DrawCurve(g, pen, visualizer.SampleData, visualizer.SampleData.Length, drawingPanel.Width, 0, drawingPanel.Height / 2, MathF.Min(drawingPanel.Height / 10, 100));

            buffer.Render();

            oldBuffer = buffer;                                   // ����һ�� buffer (֮���Բ�ȫ��ֻʹ��һ�� Buffer ����Ϊ,,, �û����ܵ������ڴ�С, ����ÿһ֡��������Ӧ)
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            capture.StartRecording();
            dataTimer.Start();
            drawingTimer.Start();
        }

        private void MainWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void DrawingPanel_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            WindowState = WindowState != FormWindowState.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
            FormBorderStyle = WindowState == FormWindowState.Maximized ? FormBorderStyle.None : FormBorderStyle.Sizable;
        }
    }
}