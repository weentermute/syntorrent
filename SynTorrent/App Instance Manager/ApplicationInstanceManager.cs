using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading;

namespace SingleInstanceApplication
{
	/// <summary>
	/// Application Instance Manager
	/// </summary>
	public static class ApplicationInstanceManager
	{
        private static EventWaitHandle eventWaitHandleArgsProcessed = null;

        /// <summary>
        /// Notify the other process that we are done processing arguments.
        /// </summary>
        public static void DoneProcessingArgs()
        {
            if (eventWaitHandleArgsProcessed != null)
                eventWaitHandleArgsProcessed.Set();
        }

		/// <summary>
		/// Creates the single instance.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="callback">The callback.</param>
		/// <returns></returns>
		public static bool CreateSingleInstance(string name, EventHandler<InstanceCallbackEventArgs> callback)
		{
			EventWaitHandle eventWaitHandle = null;
			string eventName = string.Format("{0}-{1}", Environment.MachineName, name);
            string eventNameDone = string.Format("{0}-{1}-Done", Environment.MachineName, name);

			InstanceProxy.IsFirstInstance = false;
			
            // Make this ClickOnce aware
            InstanceProxy.CommandLineArgs = CommandLineArguments.GetArguments();
            // InstanceProxy.CommandLineArgs = Environment.GetCommandLineArgs();

			try
			{
				// try opening existing wait handle
				eventWaitHandle = EventWaitHandle.OpenExisting(eventName);
                eventWaitHandleArgsProcessed = EventWaitHandle.OpenExisting(eventNameDone);
			}
			catch
			{
				// got exception = handle wasn't created yet
				InstanceProxy.IsFirstInstance = true;
			}

			if (InstanceProxy.IsFirstInstance)
			{
				// init handle
				eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
                eventWaitHandleArgsProcessed = new EventWaitHandle(false, EventResetMode.AutoReset, eventNameDone);

				// register wait handle for this instance (process)
				ThreadPool.RegisterWaitForSingleObject(eventWaitHandle, WaitOrTimerCallback, callback, Timeout.Infinite, false);
				eventWaitHandle.Close();

				// register shared type (used to pass data between processes)
				RegisterRemoteType(name);
			}
			else
			{
				// Pass console arguments to shared object
				UpdateRemoteObject(name);

				// Invoke (signal) wait handle on other process and wait until other 
                // process has processed the arguments.
                // The other process will signal eventWaitHandleArgsProcessed once it is done processing the arguments
                // unblocking this thread.
                if (eventWaitHandle != null && eventWaitHandleArgsProcessed != null)
                {
                    // SignalAndWaitonly only works on MTA threads, which is not the case in WPF!
                    // EventWaitHandle.SignalAndWait(eventWaitHandle, eventWaitHandleArgsProcessed);
                    eventWaitHandle.Set();
                    eventWaitHandleArgsProcessed.WaitOne();
                }
				// Exit current process
				Environment.Exit(0);
			}

			return InstanceProxy.IsFirstInstance;
		}

		/// <summary>
		/// Updates the remote object.
		/// </summary>
		/// <param name="uri">The remote URI.</param>
		private static void UpdateRemoteObject(string uri)
		{
			// register net-pipe channel
			var clientChannel = new IpcClientChannel();
			ChannelServices.RegisterChannel(clientChannel, true);

			// get shared object from other process
			var proxy =
				Activator.GetObject(typeof(InstanceProxy), 
				string.Format("ipc://{0}{1}/{1}", Environment.MachineName, uri)) as InstanceProxy;

			// pass current command line args to proxy
			if (proxy != null)
				proxy.SetCommandLineArgs(InstanceProxy.IsFirstInstance, InstanceProxy.CommandLineArgs);

			// close current client channel
			ChannelServices.UnregisterChannel(clientChannel);
		}

		/// <summary>
		/// Registers the remote type.
		/// </summary>
		/// <param name="uri">The URI.</param>
		private static void RegisterRemoteType(string uri)
		{
			// register remote channel (net-pipes)
			var serverChannel = new IpcServerChannel(Environment.MachineName + uri);
			ChannelServices.RegisterChannel(serverChannel, true);

			// register shared type
			RemotingConfiguration.RegisterWellKnownServiceType(
				typeof(InstanceProxy), uri, WellKnownObjectMode.Singleton);

			// close channel, on process exit
			Process process = Process.GetCurrentProcess();
			process.Exited += delegate { ChannelServices.UnregisterChannel(serverChannel); };
		}

		/// <summary>
		/// Wait Or Timer Callback Handler
		/// </summary>
		/// <param name="state">The state.</param>
		/// <param name="timedOut">if set to <c>true</c> [timed out].</param>
		private static void WaitOrTimerCallback(object state, bool timedOut)
		{
			// cast to event handler
			var callback = state as EventHandler<InstanceCallbackEventArgs>;
			if (callback == null) return;

			// invoke event handler on other process
			callback(state,
					 new InstanceCallbackEventArgs(InstanceProxy.IsFirstInstance,
												   InstanceProxy.CommandLineArgs));
		}
	}
}