﻿using System;

namespace Core.DomainModel
{
    /// <summary>
    /// Base entity class.
    /// Every domain model should extend this.
    /// </summary>
    public abstract class Entity : IEntity
    {
        protected Entity()
        {
            // instance creation time
            LastChanged = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets the primary identifier.
        /// </summary>
        /// <value>
        /// The primary identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the object author <see cref="User"/> identifier.
        /// </summary>
        /// <value>
        /// The object owner <see cref="User"/> identifier.
        /// </value>
        /// <remarks>
        /// Note that type must be nullable as <see cref="User"/>
        /// needs it to be optional.
        /// </remarks>
        public int? ObjectOwnerId { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="User"/> that authored this instance.
        /// </summary>
        /// <value>
        /// The object owner.
        /// </value>
        public virtual User ObjectOwner { get; set; }

        /// <summary>
        /// Gets or sets the DateTime of when the last change occurred to this instance.
        /// </summary>
        /// <value>
        /// Datetime of when the last change occurred.
        /// </value>
        public DateTime LastChanged { get; set; }

        /// <summary>
        /// Gets or sets the User identifier for <see cref="LastChangedByUser"/>
        /// </summary>
        /// <remarks>
        /// Note that type must be nullable as <see cref="User"/>
        /// needs it to be optional
        /// </remarks>
        public int? LastChangedByUserId { get; set; }
        /// <summary>
        /// Gets or sets the User which made the most recent change to this instance.
        /// </summary>
        /// <value>
        /// The last User which made changes to this instance.
        /// </value>
        public virtual User LastChangedByUser { get; set; }
    }
}
