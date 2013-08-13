using System;
using Raven.Abstractions.Exceptions;
using Raven.Client;
using Raven.Client.Document;

namespace NSemble.Core.Tasks
{
	public abstract class ExecutableTask
	{
	    public IDocumentStore RavenDocumentStore { get; protected set; }
	    protected IDocumentSession DocumentSession;

	    protected virtual void Initialize(IDocumentSession session)
		{
	        if (session == null) return;
	        DocumentSession = session;
	        DocumentSession.Advanced.UseOptimisticConcurrency = true;
		}

		protected virtual void OnError(Exception e)
		{
		}

		public bool? Run()
		{
		    IDocumentSession openSession = null;
            if (RavenDocumentStore != null)
            {
                openSession = RavenDocumentStore.OpenSession();
            }

			Initialize(openSession);
			try
			{
				Execute();
				DocumentSession.SaveChanges();
				TaskExecutor.StartExecuting();
				return true;
			}
			catch (ConcurrencyException e)
			{
				OnError(e);
				return null;
			}
			catch (Exception e)
			{
				OnError(e);
				return false;
			}
			finally
			{
                if (openSession != null) openSession.Dispose();
				TaskExecutor.Discard();
			}
		}

		public abstract void Execute();
	}

}
