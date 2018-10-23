using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MARC.HI.EHRS.CR.Security.Services.Impl
{

    /// <summary>
    /// Memory session manager service
    /// </summary>
    public class MemorySessionManagerService : ISessionManagerService
    {

        /// <summary>
        /// Sessions 
        /// </summary>
        protected Dictionary<Guid, SessionInfo> m_session = new Dictionary<Guid, SessionInfo>();

        /// <summary>
        /// Deletes the specified session
        /// </summary>
        public SessionInfo Delete(Guid sessionId)
        {
            SessionInfo ses = null;
            if (this.m_session.TryGetValue(sessionId, out ses))
                this.m_session.Remove(sessionId);
            return ses;
        }

        /// <summary>
        /// Establish a session for the principal provided
        /// </summary>
        public virtual SessionInfo Establish(IPrincipal principal)
        {
            lock (this.m_session)
            {
                var ses = new SessionInfo(principal);
                this.m_session.Add(ses.Key, ses);
                return ses;
            }
        }

        /// <summary>
        /// Get the specified session
        /// </summary>
        public virtual SessionInfo Get(Guid sessionId)
        {
            SessionInfo ses = null;
            if (!this.m_session.TryGetValue(sessionId, out ses))
                return null;
            return ses;
        }

        /// <summary>
        /// Get session by the session token
        /// </summary>
        /// <param name="sessionToken">The session token</param>
        /// <returns>The session information</returns>
        public virtual SessionInfo Get(String sessionToken)
        {
            return this.m_session.Values.FirstOrDefault(o => o.Token == sessionToken);
        }

        /// <summary>
        /// Get active session based on the session identity
        /// </summary>
        public virtual IEnumerable<SessionInfo> GetActive(string userName)
        {
            return this.m_session.Values.Where(o => o.Principal.Identity.Name == userName);
        }

        /// <summary>
        /// Refreshes the specified session
        /// </summary>
        public virtual SessionInfo Refresh(SessionInfo session, String password)
        {

            if (session == null) return session;

            var idp = ApplicationContext.Current.GetService<IIdentityProviderService>();

            // First is this a valid session?
            if (!this.m_session.ContainsKey(session.Key))
                throw new KeyNotFoundException();

            var principal = idp.Authenticate(session.Principal, password);
            if (principal == null)
                throw new SecurityException("Could not extend session!");
            else
            {
                var newSession = new SessionInfo(principal);
                if (!this.m_session.ContainsKey(session.Key))
                {
                    newSession.Key = Guid.NewGuid();
                    this.m_session.Add(newSession.Key, newSession);
                }
                else
                {
                    newSession.Key = session.Key;
                    this.m_session[session.Key] = newSession;
                }
                return session;
            }
        }
    }
}
