using System;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Loki.Utils
{
    /// <summary>
    /// Simple WCF client to manage communication errors
    /// </summary>
    /// <typeparam name="T">WCF service interface</typeparam>
    public class WcfClient<T> : IDisposable
        where T : class
    {
        /// <summary>
        /// WCF channel proxy
        /// </summary>
        public WcfChannelProxy<T> Proxy { get; private set; }

        /// <summary>
        /// Entry point to execute WCF methods
        /// </summary>
        public T Call { get; private set; }

        #region Constructors

        /// <summary>
        /// Simple WCF client to manage communication errors
        /// </summary>
        /// <param name="channelFactory">WCf Channel Factory</param>
        public WcfClient(ChannelFactory<T> channelFactory)
        {
            InitProxy(channelFactory);
        }

        /// <summary>
        /// Create a wcf channel
        /// </summary>
        /// <param name="endpointConf">Service Endpoint configuration</param>
        public WcfClient(String endpointConf = null)
        {
            var conf = endpointConf ?? WcfHelper.ResolveEndpoint(x => typeof(T).FullName.EndsWith(x.Contract)).Name;
            var chFactory = new ChannelFactory<T>(conf);
            InitProxy(chFactory);
        }

        /// <summary>
        /// Create a wcf duplex channel
        /// </summary>
        /// <param name="callback">Callback object</param>
        /// <param name="endpointConf">Service Endpoint configuration</param>
        public WcfClient(object callback, String endpointConf = null)
        {
            var conf = endpointConf ?? WcfHelper.ResolveEndpoint(x => typeof(T).FullName.EndsWith(x.Contract)).Name;
            var chFactory = new DuplexChannelFactory<T>(callback, conf);
            InitProxy(chFactory);
        }

        /// <summary>
        /// Create a wcf channel
        /// </summary>
        /// <param name="binding">Service wcf binding</param>
        /// <param name="endpointAddress">Service Endpoint address</param>
        public WcfClient(Binding binding, EndpointAddress endpointAddress)
        {
            var chFactory = new ChannelFactory<T>(binding, endpointAddress);
            InitProxy(chFactory);
        }

        /// <summary>
        /// Create a wcf duplex channel
        /// </summary>
        /// <param name="callback">Callback object</param>
        /// <param name="binding">Service wcf binding</param>
        /// <param name="endpointAddress">Service Endpoint address</param>
        public WcfClient(object callback, Binding binding, EndpointAddress endpointAddress)
        {
            var chFactory = new DuplexChannelFactory<T>(callback, binding, endpointAddress);
            InitProxy(chFactory);
        }

        private void InitProxy(ChannelFactory<T> chFactory)
        {
            Proxy = new WcfChannelProxy<T>(chFactory);
            Call = (T)Proxy.GetTransparentProxy();
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose the WCF client and close channel
        /// </summary>
        public void Dispose()
        {
            Proxy.CloseChannel();
        }

        #endregion
    }


    /// <summary>
    /// Proxy to manage WCF Channel connections, and auto recreate them on communication errors
    /// </summary>
    /// <typeparam name="T">WCF channel interface</typeparam>
    public class WcfChannelProxy<T> : RealProxy
        where T : class
    {

        /// <summary>
        /// WcfChannelProxy constructor
        /// </summary>
        /// <param name="channelFactory">System.ServiceModel.ChannelFactory used for creating the wcf channels</param>
        public WcfChannelProxy(ChannelFactory<T> channelFactory)
            : base(typeof(T))
        {
            _chFactory = channelFactory;
            NbRetriesOnWcfException = 1;    // needed to reopen Channel Faulted
            NbRetriesOnException = 0;
        }

        /// <summary>
        /// Number of retries, on WCF communication exception only
        /// </summary>
        public int NbRetriesOnWcfException { get; set; }

        /// <summary>
        /// Number of retries, for other exceptions
        /// </summary>
        public int NbRetriesOnException { get; set; }

        /// <summary>
        /// Get proxified channel
        /// </summary>
        public ICommunicationObject Channel { get { return _innerChannel; } }

        /// <summary>
        /// Notify that a new channel was created
        /// </summary>
        public event Action<ICommunicationObject> NewChannel;

        /// <summary>
        /// Notify that a channel was closed
        /// </summary>
        public event Action<ICommunicationObject> ChannelClosed;

        /// <summary>
        /// Notify that the method is called again, because a communication exception was caught
        /// </summary>
        public event Action<CommunicationException> RetryOnCommunicationException;

        /// <summary>
        /// Notify that the method is called again, because an exception was caught
        /// </summary>
        public event Action<Exception> RetryOnException;



        #region Inner Channel

        private ChannelFactory<T> _chFactory;
        private ICommunicationObject _innerChannel;
        private RealProxy _innerChannelProxy; // Note that System.ServiceModel.ChannelFactory already returns a RealProxy

        /// <summary>
        /// Open a new proxified channel
        /// </summary>
        private void OpenChannel()
        {
            try
            {
                if (_innerChannel == null)
                {
                    // Recreate WCF channel
                    // Note that System.ServiceModel.ChannelFactory already returns a RealProxy 
                    _innerChannel = _chFactory.CreateChannel() as ICommunicationObject;
                    _innerChannelProxy = RemotingServices.GetRealProxy(_innerChannel);

                    // Notify that a new channel was created
                    if (NewChannel != null)
                        NewChannel(_innerChannel);
                }
            }
            catch (Exception)
            {
                // TODO
            }
        }

        /// <summary>
        /// Close the proxified channel.
        /// 
        /// </summary>
        public void CloseChannel()
        {
            try
            {
                if (_innerChannel != null)
                {
                    try
                    {
                        _innerChannel.Close();
                    }
                    catch (CommunicationException)
                    {
                        _innerChannel.Abort();
                    }
                    catch (TimeoutException)
                    {
                        _innerChannel.Abort();
                    }
                    catch (Exception)
                    {
                        _innerChannel.Abort();
                    }

                    // Notify that the inner channel was closed
                    if (ChannelClosed != null)
                        ChannelClosed(_innerChannel);
                }
            }
            catch (Exception)
            {
                // TODO -> needed?
            }
            finally
            {
                _innerChannel = null;
                _innerChannelProxy = null;
            }
        }

        /// <summary>
        /// Test if the channel is opened and ready.
        /// </summary>
        /// <returns>
        /// True if the channel is opened and ready, false otherwize. 
        /// Be carreful, a fault channel can return true! because the fault state is detecting during the wcf call.
        /// </returns>
        private bool IsChannelUsable()
        {
            if (_innerChannel == null)
                return false;

            var state = _innerChannel.State;
            return state == CommunicationState.Opened || state == CommunicationState.Created;
        }

        #endregion

        #region RealProxy implementation

        public override IMessage Invoke(IMessage msg)
        {
            int nbWcfExceptions = 0;
            int nbExceptions = 0;

            while (true)
            {
                // Ensure channel is open
                if (!IsChannelUsable())
                {
                    CloseChannel();
                    OpenChannel();
                }

                // Call Channel method
                var result = _innerChannelProxy.Invoke(msg) as IMethodReturnMessage;

                // Everything ok
                if (result.Exception == null)
                    return result;

                // Manage Exceptions
                if (result.Exception is CommunicationException)
                {
                    nbWcfExceptions++;

                    // WCF Communication exception (faulted channel for example)
                    if (nbWcfExceptions > NbRetriesOnWcfException)
                        return result;

                    // Notify exception
                    if (RetryOnCommunicationException != null)
                        RetryOnCommunicationException(result.Exception as CommunicationException);
                }
                else
                {
                    nbExceptions++;

                    // Not an communication exception
                    if (nbExceptions > NbRetriesOnException)
                        return result;

                    // Notify exception
                    if (RetryOnException != null)
                        RetryOnException(result.Exception);
                }
            }
        }


        #endregion
    }


}
