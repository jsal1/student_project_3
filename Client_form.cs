using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace client_side
{
    public partial class Client_form : Form
    {
        Socket userSocket;
        Thread listenerThread;
        string my_id="null";
        bool connect_to_server = false;
        IPAddress ipForForm2;
        Client_remote client_Remote;
        public Client_form()
        {
            InitializeComponent();
            my_id = Guid.NewGuid().ToString();
            my_id = my_id.Remove(7);
            label3.Text += my_id;
        }
        //кнопка сервера
        private void con_server_Click(object sender, EventArgs e)
        {
            try
            {
                IPAddress ip = IPAddress.Parse(textBox1.Text);//считываем айпи
                int port = int.Parse(textBox2.Text);//считываем порт
                userSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                userSocket.Connect(new IPEndPoint(ip, port));
                if (userSocket.Connected)
                {
                    MessageBox.Show("Связь с сервером установлена.");
                    listenerThread = new Thread(listen);
                    listenerThread.IsBackground = true;
                    listenerThread.Start();
                    Send("reg#"+my_id);
                }
            }
            catch (Exception ex)
            { MessageBox.Show("err  " + ex.Message); }
        }
        //отправка команды клиента на сервер
        private void Send(string buf)
        {
            try
            {
                userSocket.Send(Encoding.Default.GetBytes(buf));
            }
            catch { }
        }

        //слушаем сервер
        private void listen()
        {
            try
            {
                while(userSocket.Connected)
                {
                    byte[] buffer = new byte[1024];
                    int bytes = userSocket.Receive(buffer);
                    handleCom(Encoding.Default.GetString(buffer, 0, bytes));
                }
            }
            catch
            {
                MessageBox.Show("Связь прервана, презапустите");
                Application.Exit();
            }
        }
        //команды от сервера
        private void handleCom(string command)
        {
            try
            {
                if (command.StartsWith("reg"))
                {
                    if (command.Equals("regfalse"))
                    {
                        MessageBox.Show("этот пользователь уже подключен");
                        Application.Exit();
                    }
                    if (command.Equals("regtrue"))
                    {
                        connect_to_server = true;
                        MessageBox.Show("Успешное подключение");
                        Invoke((MethodInvoker)delegate
                        {
                            textBox1.ReadOnly = true;
                            textBox2.ReadOnly = true;
                            con_server.Enabled = false;
                        });
                    }
                    return;
                }
                if (command.StartsWith("usernofound"))
                {
                    MessageBox.Show("Пользователь " + command.Substring(command.IndexOf('#')+1,
                        command.LastIndexOf('#')- command.IndexOf('#') -1) +" не найден!");
                    return;
                }
                if (command.StartsWith("block"))
                {
                    string id_conn = command.Substring(command.IndexOf('#') + 1,
                        command.LastIndexOf('#') - command.IndexOf('#') - 1);
                    MessageBox.Show("Клиент "+id_conn+" занят\r\nПопробуйте позже!");
                    return;
                }
                if (command.StartsWith("connect"))
                {
                    //запрос от пользователя *
                    string id_conn = command.Substring(command.IndexOf('#') + 1,
                        command.LastIndexOf('#') - command.IndexOf('#') - 1);
                    //диалог с ним
                    DialogResult DR = MessageBox.Show("Запрос на подключение от  " + id_conn
                        + "\r\nРазрешить?", "Запрос", MessageBoxButtons.YesNo);
                    if (DR == DialogResult.Yes)
                    {
                        Send("answeryes#" + id_conn+"#");
                        //инициализация потока для отправки скриншотов в этом потоке
                        new sender();
                    }
                    else if (DR == DialogResult.No || DR == DialogResult.None)
                    {
                        Send("answerno#" + id_conn + "#");
                    }
                    else {      }
                    return;
                }
                if (command.StartsWith("answer"))
                {
                    if (command.StartsWith("answeryes"))
                    {
                        //id
                        string id_remote = command.Substring(command.IndexOf('#') + 1,
                        command.IndexOf('&') - command.IndexOf('#') - 1);
                        //ip
                        ipForForm2 = IPAddress.Parse(command.Substring(command.IndexOf('&')+1, 
                            command.LastIndexOf('#')- command.IndexOf('&')-1));
                        MessageBox.Show("Пользователь " + id_remote + " принял подключение");
                        //инициализация принятия скриншотов через форму 2, новый поток
                        Invoke((MethodInvoker)delegate
                        {
                            client_Remote = new Client_remote(ipForForm2);
                            client_Remote.Show();
                        });

                    }
                    else if (command.StartsWith("answerno"))
                    {
                        string id_remote = command.Substring(command.IndexOf('#') + 1,
                           command.LastIndexOf('#') - command.IndexOf('#') - 1);
                        MessageBox.Show("Пользователь " + id_remote + " отклонил подключение");
                    }
                    return;
                }
            }
            catch { }
        }
        
        //обработчик при завершении
        private void Client_form_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (client_Remote==null)
                {
                    client_Remote.Close();
                }
                if (userSocket.Connected&& connect_to_server)
                {
                    Send("exit");
                    userSocket.Close();
                }
                else if (userSocket.Connected && !connect_to_server)
                    userSocket.Close();
            }
            catch { }
        }
        //кнопка клиента
        private void con_client_Click(object sender, EventArgs e)
        {
            try
            {
                string id_remote = textBox3.Text;
                if (my_id == id_remote)
                {
                    MessageBox.Show("Нельзя подключиться к себе");
                }
                else
                {
                    if (userSocket.Connected)
                    {
                        Send("connect#" + id_remote + "#");
                    }
                    else
                    {
                        MessageBox.Show("Нет подключения к серверу");
                        Application.Exit();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("err  " + ex.Message); }
        }
    }
    class sender //класс для работы с отправкой скриншотов
    {
        TcpClient tcpClient;
        Thread listener;
        Thread reseiver;
        NetworkStream stream;
        public sender()
        {
            TcpListener Listener = new TcpListener(IPAddress.Any,11111);
            Listener.Start();
            tcpClient = Listener.AcceptTcpClient();
            reseiver = new Thread(reseive);
            reseiver.IsBackground = true;
            reseiver.Start();
            listener = new Thread(list);
            listener.IsBackground = true;
            listener.Start();
        }
        void reseive()
        {
            try
            {
                while (tcpClient.Connected)
                {
                    BinaryFormatter binFormatter = new BinaryFormatter();
                    stream = tcpClient.GetStream();
                    binFormatter.Serialize(stream, graph());
                    //Thread.Sleep(100);
                }
            }
            catch { MessageBox.Show("Соединение разорвано"); listener.Abort(); }
        }
        private object graph()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap screenshot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(screenshot);
            graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            return screenshot;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        void list()
        {
            while (tcpClient.Connected)
            {
                if (stream.DataAvailable)
                {
                    byte[] buffer = new byte[50];
                    string message = string.Empty;
                    do
                    {
                        stream.Read(buffer, 0, buffer.Length);
                        message += Encoding.Default.GetString(buffer);
                    }
                    while (stream.DataAvailable);
                    //----------------------------------------
                    if (message.StartsWith("move")) 
                    {
                        string[] temp = message.Split('#');
                        Cursor.Position = new Point(int.Parse(temp[1]), int.Parse(temp[2]));
                    }
                    else if (message.StartsWith("click"))
                    {
                        string[] temp = message.Split('#');
                        if (temp[1].Equals("DownLeft"))
                            sendMouseLeftDown();
                        else if (temp[1].Equals("UpLeft"))
                            sendMouseLeftUp();
                        else if(temp[1].Equals("DownRight"))
                          sendMouseRightDown();
                        else if(temp[1].Equals("UpRight"))
                            sendMouseRightUp();
                    }
                    else if (message.StartsWith("key")) 
                    {
                        string[] temp = message.Split('#');
                        PressKey(temp[1]);
                    }
                }
            }
        }
        void PressKey(string keybutton)
        {
            if (keybutton == " " || keybutton == "\r")
                SendKeys.SendWait(keybutton);
            else
            {
                SendKeys.SendWait("{" + keybutton + "}");
            }
        }
        void sendMouseLeftDown()//1
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        }
        void sendMouseLeftUp()//2
        {
            mouse_event(MOUSEEVENTF_LEFTUP , 0, 0, 0, UIntPtr.Zero);
        }
        void sendMouseRightDown()//3
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
        }
        void sendMouseRightUp()//4
        {
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }
}
