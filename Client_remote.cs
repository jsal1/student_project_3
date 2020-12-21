using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace client_side
{
    public partial class Client_remote : Form
    {
        Thread listenerThread;
        IPAddress ip_remote;
        TcpClient tcpClient = new TcpClient();
        NetworkStream stream;
        public Client_remote(IPAddress ip_r)
        {
            ip_remote = ip_r;
            InitializeComponent();
            listenerThread = new Thread(reseiver);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }
        private void reseiver()
        {
            try
            {
                tcpClient.Connect(new IPEndPoint(ip_remote, 11111));
                BinaryFormatter binFormatter = new BinaryFormatter();
                while (tcpClient.Connected)
                {
                    stream = tcpClient.GetStream();
                    pictureBox1.Image = (Image)binFormatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("err  " + ex.Message);
            }
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            byte[] buffer = Encoding.Default.GetBytes("move#" + e.X + "#" + e.Y + "#");
            try
            {
                if (tcpClient.Connected)
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch { }
        }
        private void Client_remote_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (tcpClient != null)
                tcpClient.Close();
            Invoke((MethodInvoker)delegate
            {
                Close();
            });
        }
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            string message = string.Empty;
            if (e.Button == MouseButtons.Left)
            {
                 message = "click#DownLeft#";// Одно нажатие левой
            }
            if (e.Button == MouseButtons.Right)
            {
                message = "click#DownRight#";// Одно нажатие правой
            }
            byte[] buffer = Encoding.Default.GetBytes(message);
            try
            {
                if (tcpClient.Connected)
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch { }
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            string message = string.Empty;
            if (e.Button == MouseButtons.Left)
            {
                message = "click#UpLeft#";// Одно нажатие левой
            }
            if (e.Button == MouseButtons.Right)
            {
                message = "click#UpRight#";// Одно нажатие левой
            }
            byte[] buffer = Encoding.Default.GetBytes(message);
            try
            {
                if (tcpClient.Connected)
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch { }
        }
        private void Client_remote_KeyPress(object sender, KeyPressEventArgs e)
        {
            byte[] buffer = Encoding.Default.GetBytes("key#"+e.KeyChar + "#");
            try
            {
                if (tcpClient.Connected)
                {
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
            catch { }
        }
    }
}
