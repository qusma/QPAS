// -----------------------------------------------------------------------
// <copyright file="SerializableKVP.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QPAS
{
    [Serializable]
    public struct SerializableKvp<TK, TV>
    {
        private TK _key;
        private TV _value;

        public TK Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public TV Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public SerializableKvp(TK key, TV value)
        {
            _key = key;
            _value = value;
        }
    }
}