using System;
using Grouper2.Utility;

namespace Grouper2.Host.DcConnection
{
    public partial class Ldap
    {
        // shit to make this a thread safe singleton and save some headaches
        private static Ldap _netconn;
        private static bool _initialised;
        private bool _hasBeenCollected;
        private static object syncLock = new object();

        public static Ldap Use()
        {
            // early return clauses
            if (!_initialised)
            {
                Ldap.Connect(JankyDb.Vars.OnlineMode, JankyDb.Vars.Domain, JankyDb.Vars.Interest);
            }

            if (_netconn != null)
            {
                // set up the locking for multithreading
                lock (syncLock)
                {
                    if (_netconn == null)
                    {
                        // if this wasn't set up at the start of execution,
                        // provide a neutered version
                        // it'd be strange to end up here though cause of _initialised?
                        _netconn = new Ldap(false, "", 100);
                    }
                }
            }

            return _netconn;
        }

        /// <summary>
        /// Build OR REBUILDS! the ldap connection singleton.
        ///
        /// This will cause a data retrieval from the domain as the connection builds a local cache
        /// </summary>
        /// <param name="onlineMode">false if ldap connection should be minimised</param>
        /// <param name="domain">the domain to connect to</param>
        /// <param name="desiredInterestLevel">the interest level to stop processing at</param>
        /// <returns></returns>
        private static Ldap Connect(bool onlineMode, string domain, int desiredInterestLevel)
        {
            // this singleton can be rebuilt from scratch. not sure if this is good,
            // but it might make it easier to process multi-domain environments later?
            // double lock pattern engage!!!
            if (_netconn == null)
            {
                // alright, let's lock here and build the new one
                lock (syncLock)
                {
                    if (_netconn == null)
                    {
                        // build the thing
                        try
                        {
                            _netconn = new Ldap(onlineMode, domain, desiredInterestLevel);
                        }
                        catch (Exception e)
                        {
                            Log.Degub("Unable to establish a network object", e, _netconn);
                            throw;
                        }
                        
                        // get the stuff
                        _initialised = false;
                        _netconn._hasBeenCollected = false;
                        try
                        {
                            _netconn.CollectOnlineData();
                        }
                        catch (Exception e)
                        {
                            
                            Log.Degub("Unable to establish a network object", e, _netconn);
                            throw;
                        }
                        _netconn._hasBeenCollected = true;
                        _initialised = true;
                    }
                }
            }
            // all this bullshit could reinit the thing
            /*else
            {
                lock (syncLock)
                {
                    // reeset the shit
                    _netconn = null;

                    /// build the thing
                    try
                    {
                        _netconn = new Ldap(onlineMode, domain, DesiredInterestLevel);
                    }
                    catch (Exception e)
                    {
                        
                        Log.Degub("TODO:", e, this);
                        throw;
                    }

                    // get the stuff
                    _initialised = false;
                    _netconn._hasBeenCollected = false;
                    try
                    {
                        _netconn.CollectOnlineData();
                    }
                    catch (Exception e)
                    {
                        
                        Log.Degub("TODO:", e, this);
                        throw;
                    }
                    _netconn._hasBeenCollected = true;
                    _initialised = true;
                }
            }*/

            // yay!
            return _netconn;
        }
    }
}