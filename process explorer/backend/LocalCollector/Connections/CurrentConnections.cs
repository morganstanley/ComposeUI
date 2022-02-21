
namespace ProcessExplorer.Entities.Connections
{
    public class ConnectionMonitor 
    {
        public List<IConnection> connections;
        public ConnectionMonitor()
        {
            connections = new List<IConnection>();
        }
        public void AddConnection(ref IConnection connectionInfo)
            => connections.Add(connectionInfo);
        public void RemoveConnection(ref IConnection connectionInfo)
            => connections.Remove(connectionInfo);
        public void ChangeElement(IConnection connection)
        {
            var element = connections.FindIndex(conn => conn.Id == connection.Id);
            if (element != default)
            {
                connections[element] = connection;
            }
            else
            {
                connections.Add(connection);
            }
        }
        public ConnectionMonitor(List<IConnection> connections)
            => this.connections = connections;
        public IConnection? GetConnection(IConnection connection) 
            => connections.Where(conn => conn.Equals(connection)).FirstOrDefault();

        public List<IConnection>? GetConnections()
            => connections;

        public void StatusChanged(ref IConnection conn)
        {
            throw new NotImplementedException(); //triggering event tbc
        }
    }
}
