using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using Common.interfaces;
using ProtoBuf;
using UnitOfWork;

namespace Domain
{
    /// <summary>
    /// Implements <see cref="INotifyPropertyChanged"/> but adds event bubbling to notify changes in 
    /// instances it references to.
    /// </summary>
    [ProtoContract]
    [DebuggerDisplay("{ToInfo()}")]
    public abstract class DomainBase : Changeable, IInfo
    {
        private Guid _id = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ProtoMember(1)]
        public Guid Id
        {
            get { return _id; }
            set { SetProperty(() => Id, value, ref _id); }
        }

        /// <summary>
        /// Finds the equal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static T FindEqual<T>(IEnumerable<T> enumerable, T item) where T : DomainBase
        {
            return item == null ? null : enumerable.FirstOrDefault(item.Equals);
        }

        /// <summary>
        /// Checks if the enumable contains the item using the <see cref="FindEqual&lt;T&gt;"/> method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="item">The item.</param>
        /// <returns>True if the item is contained in the enumerable, otherwise false. Null is never contained in the enumerable.</returns>
        public static bool ContainsEqual<T>(IEnumerable<T> enumerable, T item) where T : DomainBase
        {
            return FindEqual(enumerable, item) != null;
        }

        /// <summary>
        /// Determines whether the specified a is equal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a">The a.</param>
        /// <param name="b">The b.</param>
        /// <returns>
        /// 	<c>true</c> if the specified a is equal; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEqual<T>(T a, T b) where T : DomainBase
        {
            if ((a == null) && (b == null)) return true;
            return (a != null) && a.Equals(b);
        }

        #region IEquatable<DomainBase> Members

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same Type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        public override bool Equals(Changeable other)
        {
            return (other is DomainBase) && Id.Equals(((DomainBase)other).Id);
        }

        #endregion

        #region IInfo Members

        /// <summary><see cref="IInfo.ToInfo()"/></summary>
        public string ToInfo()
        {
            return ToInfo(false);
        }

        /// <summary>
        /// <see cref="IInfo.ToInfo()"/>
        /// </summary>
        /// <returns>
        /// A string containing the Type fullname on both long or short information
        /// </returns>
        public virtual string ToInfo(bool shortInfo)
        {
            return string.Format("{0}, Id: {1}", GetType().FullName, Id);
        }

        /// <summary>
        /// <see cref="IInfo.ToInfo(IInfo)"/>
        /// This implementation returns the short info of the IInfo if not null, the string "null" if null
        /// </summary>
        public string ToInfo(IInfo info)
        {
            return info == null ? "null" : info.ToInfo();
        }

        #endregion
    }
}
