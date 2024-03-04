using Newtonsoft.Json.Linq;
using WebSocketServer.Utilities;
using WebSocketServer.DataService.Utiities;

using static WebSocketServer.ServiceLogic.MasterSlavesGroupService;

namespace WebSocketServer.DataService
{
    internal class MasterSlavesGroupData
    {
        // todo 修改一下数据结构
        //private TreeItem<IClient>.Collection collection = new TreeItem<IClient>.Collection();

        private HashSet<string> registeredListeners = new HashSet<string>();

        private Dictionary<string, MasterClient> registeredMasters = new Dictionary<string, MasterClient>();

        private Dictionary<string, SlaveClient> registeredSlaves  = new Dictionary<string, SlaveClient>();

        internal async Task<bool> RegisterListener(string clientId)
        {
            if (registeredListeners.Contains(clientId))
            {
                return await Task.FromResult(false);
            }
            registeredListeners.Add(clientId);
            return await Task.FromResult(true);
        }

        internal async Task<bool> UnregisterListener(string clientId)
        {
            if (!registeredListeners.Contains(clientId))
            {
                return await Task.FromResult(false);
            }
            registeredListeners.Remove(clientId);
            return await Task.FromResult(true);
        }

        internal async Task<IEnumerable<string>> GetAllListeners()
        {           
            return await Task.FromResult(registeredListeners);
        }

        /// <summary>
        /// 获取所有 master
        /// </summary>
        /// <returns></returns>
        internal async Task<IEnumerable<MasterClient>> GetAllMasters()
        {
            //return (IEnumerable<MasterClient>)await Task.FromResult(collection.GetAllItems().Where(p => p?.GetType() == typeof(MasterClient)));
            return await Task.FromResult(registeredMasters.Values.ToArray());
        }

        /// <summary>
        /// Get a registered provider by id
        /// </summary>
        internal async Task<MasterClient?> GetMasterByClientId(string id)
        {
            if (registeredMasters.ContainsKey(id))
            {
                return await Task.FromResult<MasterClient?>(registeredMasters[id]);
            }
            else
            {
                return await Task.FromResult<MasterClient?>(null);
            }
        }

        /// <summary>
        /// Register a provider
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        internal async Task RegisterMaster(MasterClient? master)
        {
            if (master != null && !string.IsNullOrEmpty(master.clientId))
            {
                registeredMasters[master.clientId] = master;               
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Unregister a provider
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        internal async Task UnregisterMaster(MasterClient? master)
        {
            if (master != null && !string.IsNullOrEmpty(master.clientId) && registeredMasters.ContainsKey(master.clientId))
            {
                registeredMasters.Remove(master.clientId);
            }
            await Task.CompletedTask;            
        }

        /// <summary>
        /// 通过 Id 获取一个Slave
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal async Task<SlaveClient?> GetSlaveByClientId(string id)
        {
            if (registeredSlaves.ContainsKey(id))
            {
                return await Task.FromResult<SlaveClient?>(registeredSlaves[id]);
            }
            else
            {
                return await Task.FromResult<SlaveClient?>(null);
            }
        }

        /// <summary>
        /// 注册一个 slave
        /// </summary>
        /// <param name="slave"></param>
        /// <returns></returns>
        internal async Task<bool> RegisterSlave(SlaveClient? slave)
        {
            if (slave == null)
            {
                return await Task.FromResult(false);
            }
            if(registeredSlaves.ContainsKey(slave.clientId))
            {
                return await Task.FromResult(false);
            }
            registeredSlaves.Add(slave.clientId, slave);
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 注销一个 slave
        /// </summary>
        /// <param name="slave"></param>
        /// <returns></returns>
        internal async Task<bool> UnregisterSlave(SlaveClient? slave)
        {
            if (slave == null)
            {
                return await Task.FromResult(false);
            }
            if (registeredSlaves.ContainsKey(slave.clientId))
            {
                registeredSlaves.Remove(slave.clientId);
                return await Task.FromResult(true);
            }
            return await Task.FromResult(false);
        }

        /// <summary>
        /// 获取一个 master 下的所有 slave by masterId
        /// </summary>
        /// <param name="masterId"></param>
        /// <returns></returns>
        internal async Task<IEnumerable<SlaveClient>> GetSlavesOfOneMaster(string? masterId)
        {
            /*********** 这样查找有问题，masterId不是dictionary的key,此处数据结构可以优化 ***********/
            return await Task.FromResult(registeredSlaves.Values.Where(p => p.masterId == masterId));
        }

        /// <summary>
        /// 获取所有从机
        /// </summary>
        /// <returns></returns>
        internal async Task<IEnumerable<SlaveClient>> GetAllSlaves()
        {
            return await Task.FromResult(registeredSlaves.Values);
        }
    }
}
