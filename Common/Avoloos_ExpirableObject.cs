using System;

namespace Avoloos
{
    /// <summary>
    /// This class represents an Object, which can expire.
    /// </summary>
    public class ExpirableObject
    {
        /// <summary>
        /// The time where the <see cref="Avoloos.ExpirableObject"/> was created created.
        /// </summary>
        DateTime TimeCreated;

        /// <summary>
        /// Gets or sets the expires in milliseconds.
        /// </summary>
        /// <value>The expires in given milliseconds.</value>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the expiring object.
        /// </summary>
        /// <value>The expiring object.</value>
        public object ExpiringObject { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Avoloos.ExpirableObject"/> class.
        /// </summary>
        /// <param name="expiringObject">The object which can expire.</param>
        /// <param name="expire">The time in milliseconds in which the given object will expire</param>
        public ExpirableObject(object expiringObject, int expire)
        {
            TimeCreated = DateTime.Now;
            ExpiringObject = expiringObject;
            ExpiresIn = expire;
        }

        /// <summary>
        /// Determines whether this instance is expired.
        /// </summary>
        /// <returns><c>true</c> if this instance is expired; otherwise, <c>false</c>.</returns>
        public bool IsExpired()
        {
            return DateTime.Now.Millisecond >= TimeCreated.Millisecond + ExpiresIn;
        }

        /// <summary>
        /// Will reset the expire timer, so the object will be again valid as set in the creation.
        /// </summary>
        public void ResetTime()
        {
            TimeCreated = DateTime.Now;
        }
    }
}

