using Newtonsoft.Json.Linq;
using WebSocketServer.Utilities;
using WebSocketServer.DataService.Utiities;

using static WebSocketServer.ServiceLogic.MasterSlavesGroupService;
using System.Collections.Concurrent;

namespace WebSocketServer.DataService
{
    internal class MasterSlavesGroupData
    {
        // todo 修改一下数据结构
        //private TreeItem<IClient>.Collection collection = new TreeItem<IClient>.Collection();

        private HashSet<string> registeredListeners = new HashSet<string>();

        private ConcurrentDictionary<string, MasterClient> registeredMasters = new ConcurrentDictionary<string, MasterClient>();

        private ConcurrentDictionary<string, SlaveClient> registeredSlaves  = new ConcurrentDictionary<string, SlaveClient>();

        internal async Task<bool> RegisterListener(string clientId)
        {
            bool result;
            lock (registeredListeners)
            {
                if (registeredListeners.Contains(clientId))
                {
                    result = false;
                }
                registeredListeners.Add(clientId);
                result = true;
            }
            return await Task.FromResult(result);
        }

        internal async Task<bool> UnregisterListener(string clientId)
        {
            bool result = false;
            lock (registeredListeners)
            {
                if (registeredListeners.Contains(clientId))
                {
                    registeredListeners.Remove(clientId);
                    result = true;
                }
            }
            return await Task.FromResult(result);
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
        internal async Task<bool> RegisterMaster(MasterClient? master)
        {
            if (master != null && !string.IsNullOrEmpty(master.clientId))
            {
                return await Task.FromResult(registeredMasters.TryAdd(master.clientId, master));
            }
            return false;
        }

        /// <summary>
        /// Unregister a provider
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        internal async Task<MasterClient?> UnregisterMaster(string? clientId)
        {
            if (!string.IsNullOrEmpty(clientId))
            {
                await Task.FromResult(registeredMasters.TryRemove(clientId, out MasterClient? master));
                return master;
            }
            return null;        
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
                return false;
            }            
            return await Task.FromResult(registeredSlaves.TryAdd(slave.clientId, slave));
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
                return false;
            }
            return await Task.FromResult(registeredSlaves.TryRemove(slave.clientId, out _));
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
