﻿using Flattening.Protocol.Tests.Attributes;
using Magic.Flattening.Toolkit.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flattening.Protocol.Tests.ShareDtoTests
{
    public class DummyPoint
    {
        public double DumbNumber { get; set; }
    }

    public interface IGeoLocation
    {
        public DummyPoint Location { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    /// <summary>
    /// A correctly setup flattened removed property that has both the "get" 
    /// and the "set" involving directly non removed flattened properties.
    /// </summary>
    [ShouldPass(true)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Pass1 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint() { 
                DumbNumber = (double)Longitude * (double)Latitude 
            };
            set
            {
                Longitude = (decimal)value.DumbNumber * 10;
                Latitude = (decimal)value.DumbNumber * 3;
            }
        }
    }

    /// <summary>
    /// A bad "get" but ignored
    /// </summary>
    [ShouldPass(true)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Pass2 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            [MagicOrphan]
            get => new DummyPoint()
            {
                DumbNumber = 10
            };
            set
            {
                Longitude = (decimal)value.DumbNumber * 10;
                Latitude = (decimal)value.DumbNumber * 3;
            }
        }
    }

    /// <summary>
    /// A bad "set" but ignored
    /// </summary>
    [ShouldPass(true)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Pass3 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude * (double)Latitude
            };

            [MagicOrphan]
            set
            {
            }
        }
    }

    /// <summary>
    /// A bad "get" and "set" but both "get" and "set" are ignored
    /// </summary>
    [ShouldPass(true)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Pass4 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            [MagicOrphan]
            get => null;

            [MagicOrphan]
            set
            {
            }
        }
    }

    /// <summary>
    /// A bad "get" and "set" but the property is ignored
    /// </summary>
    [ShouldPass(true)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Pass5 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        [MagicOrphan]
        public DummyPoint Location
        {
            get => null;

            set
            {
            }
        }
    }

    /// <summary>
    /// A correctly setup flattened removed property that has both the "get" 
    /// and the "set" involving indirectly non removed flattened properties.
    /// </summary>
    [ShouldPass(true)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Pass6 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get
            {
                var dummyValue = (double)Longitude * (double)Latitude;
                return new DummyPoint()
                {
                    DumbNumber = dummyValue,
                };
            }
            set
            {
                var d1 = (decimal)value.DumbNumber * 10;
                Longitude = d1;
                var d2 = (decimal)value.DumbNumber * 3;
                Latitude = d2;
            }
        }
    }

    /// <summary>
    /// A correctly setup flattened removed property that has both the "get" 
    /// and the "set" involving methods which directly and indirectly non removed flattened properties.
    /// </summary>
    [ShouldPass(true)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Pass7 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get
            {
                var p = GetDummyPoint();
                return p;
            }
            set
            {
                SetDummyPoint(value);
            }
        }

        private DummyPoint GetDummyPoint()
        {
            var dummyValue = (double)Longitude * (double)Latitude;
            return new DummyPoint()
            {
                DumbNumber = dummyValue,
            };
        }

        private void SetDummyPoint(DummyPoint dummyPoint)
        {
            var d1 = (decimal)dummyPoint.DumbNumber * 10;
            Longitude = d1;
            var d2 = (decimal)dummyPoint.DumbNumber * 3;
            Latitude = d2;
        }
    }

    /// <summary>
    /// A bad "get" that should fail
    /// </summary>
    [ShouldPass(false)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Fail1 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = 10
            };
            set
            {
                Longitude = (decimal)value.DumbNumber * 10;
                Latitude = (decimal)value.DumbNumber * 3;
            }
        }
    }

    /// <summary>
    ///  A bad "set", which should fail.
    /// </summary>
    [ShouldPass(false)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Fail2 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude * (double)Latitude
            };
            set
            {
            }
        }
    }

    /// <summary>
    /// A bag "get" and "set" that should fail
    /// </summary>
    [ShouldPass(false)]
    [MagicViewDto(typeof(IGeoLocation))]
    public class GeoLocation_Fail3 : IGeoLocation
    {
        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => null;
            set
            {
            }
        }
    }

}
