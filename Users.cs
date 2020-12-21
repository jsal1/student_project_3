using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace server_side
{
    class Users
    {
        private Thread _userThread;
        private Socket _userHandle;
        public bool block=false;
        //Инициализация сокета
        public Users(Socket handle)
        {
            //string temp = handle.RemoteEndPoint.ToString();
            //_userName = temp.Remove(temp.IndexOf(":")).Replace(".", "");
            _userHandle = handle;
            _userThread = new Thread(listner);
            _userThread.IsBackground = true;
            _userThread.Start();
        }
        //идентификатор клиента
        public string Username { get; private set; }
        //айпи клиента в строке
        public string ip_user//only ip
        {
            get {
                string temp = _userHandle.RemoteEndPoint.ToString();
                return temp.Remove(temp.IndexOf(':')); 
            }
        }
        //прослушка
        private void listner()
        {
            try
            {
                while (_userHandle.Connected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesReceive = _userHandle.Receive(buffer);//читаем
                    handleCommand(Encoding.Default.GetString(buffer, 0, bytesReceive));//передаем в обработчик
                }
            }
            catch { Server.EndUser(this); }//удаляем из списка 
        }
        //команды от клиента
        private void handleCommand(string cmd)
        {
            try
            {
                if (cmd.StartsWith("reg"))
                {
                    regUser(cmd.Split('#')[1]);
                    return;
                }
                if (cmd.Equals("exit"))
                {
                    Server.EndUser(this);
                    return;
                }
                if (cmd.StartsWith("connect"))
                {
                    string id_remote = cmd.Substring(cmd.IndexOf('#') + 1,
                        cmd.LastIndexOf('#') - cmd.IndexOf('#') - 1);
                    
                    Users usr = Server.GetUser(id_remote);
                    if (usr == null)
                    {
                        Send("usernofound#" + id_remote + "#");//ведомого нет в списке
                    }
                    else if (usr != null &&!usr.block)
                    {
                        usr.block = true;
                        usr.Send("connect#" + Username + "#");//запрос ведомому
                    }
                    else if (usr != null && usr.block)
                    {
                        Send("block#"+id_remote+"#");
                    }
                    return;
                }
                if (cmd.StartsWith("answerno"))//ответ НЕТ ведущему
                {
                    string id_conn = cmd.Substring(cmd.IndexOf('#') + 1,
                        cmd.LastIndexOf('#') - cmd.IndexOf('#') - 1);
                    Users usr = Server.GetUser(id_conn);
                    if (usr != null)
                    {
                        usr.Send("answerno#"+Username + "#");
                    }
                    return;
                }
                if (cmd.StartsWith("answeryes"))//ответ ДА ведущему 
                {
                    string id_conn = cmd.Substring(cmd.IndexOf('#') + 1,
                        cmd.LastIndexOf('#') - cmd.IndexOf('#') - 1);
                    Users usr = Server.GetUser(id_conn);
                    if (usr != null)
                    {
                        usr.Send("answeryes#" + Username + "&" + ip_user + "#");
                    }
                    return;
                }
            }
            catch { }
        }
        //регистрация клиента
        private void regUser(string username)
        {
            for (int i = 0; i < Server.UserList.Count; i++)
            {
                if (Server.UserList[i].Username == username)
                {
                    Send("regfalse");
                    End();
                    return;
                }
                if (Server.UserList[i].ip_user == ip_user)
                {
                    Send("regfalse");
                    End();
                    return;
                }
            }
            Username = username;
            Server.NewUser(this);
            Send("regtrue");
        }
        //Закрытие сокета
        public void End()
        {
            try
            {
                _userHandle.Close();
            }
            catch { }
        }
        //Отправка строки
        public void Send(string Buffer)
        {
            try
            {
                _userHandle.Send(Encoding.Default.GetBytes(Buffer));
            }
            catch { }
        }
    }
}
