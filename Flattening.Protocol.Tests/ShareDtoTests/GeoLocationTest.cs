using Truth.Protocol.Tests.Attributes;
using Magic.Truth.Toolkit.Attributes;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Truth.Protocol.Tests.ShareDtoTests
{
    public class DummyPoint
    {
        public double DumbNumber { get; set; }
    }

    [ShouldPass(true)]
    [MagicTruth]
    public interface IGeoLocationDup
    {
        public decimal Longitude { get; set; }
    }


    [ShouldPass(true)]
    [MagicTruth]
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
    [MagicMap(typeof(IGeoLocation))]
    [TruthBindingTest]
    [MapImpTest]
    public class GeoLocation_Pass1 : IGeoLocation
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
                Longitude = (decimal)value.DumbNumber * 10;
                Latitude = (decimal)value.DumbNumber * 3;
            }
        }
    }

    /// <summary>
    /// A bad "get" but ignored
    /// </summary>
    [ShouldPass(true)]
    [MagicMap(typeof(IGeoLocation))]
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
    [MagicMap(typeof(IGeoLocation))]
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
    [MagicMap(typeof(IGeoLocation))]
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
    [MagicMap(typeof(IGeoLocation))]
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
    [MagicMap(typeof(IGeoLocation))]
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
    [MagicMap(typeof(IGeoLocation))]
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
    /// Should pass even though the connected long in the getter 
    /// is removed as well because that removed property then references 
    /// an un-remved flattened property.
    /// </summary>
    [ShouldPass(true)]
    [MagicMap(typeof(IGeoLocation))]
    public class GeoLocation_Pass8 : IGeoLocation
    {
        [MagicFlattenRemove]
        public decimal Longitude
        {
            get => _Longitude; set
            {
                _Longitude = value;
            }
        }

        public decimal _Longitude { get; set; }

        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude
            };
            set
            {
                Longitude = (decimal)value.DumbNumber * 10;
                Latitude = (decimal)value.DumbNumber * 3;
            }
        }
    }

    /// <summary>
    /// Same pass as pass1, but just adding a weird dummy variable that 
    /// isn't related to the end IGeoLocation.
    /// </summary>
    [ShouldPass(true)]
    [MagicMap(typeof(IGeoLocation))]
    public class GeoLocation_Pass9 : IGeoLocation
    {
        [MagicFlattenRemove]
        public decimal DummyLongValue { get; set; }

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
                Longitude = (decimal)value.DumbNumber * 10;
                Latitude = (decimal)value.DumbNumber * 3;
            }
        }
    }

    /// <summary>
    /// DummyLongValue muddies the get but properly orphan attributed the problem
    /// </summary>
    [ShouldPass(false)]
    [MagicMap(typeof(IGeoLocation))]
    public class GeoLocation_Pass10 : IGeoLocation
    {
        [MagicOrphan]
        [MagicFlattenRemove]
        public decimal DummyLongValue { get; set; }

        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude * (double)DummyLongValue
            };
            set
            {
                Latitude = (decimal)value.DumbNumber * 4;
            }
        }
    }

    /// <summary>
    /// DummyLongValue muddies the get but properly orphan attributed the problem getter
    /// </summary>
    [ShouldPass(false)]
    [MagicMap(typeof(IGeoLocation))]
    public class GeoLocation_Pass11 : IGeoLocation
    {
        
        [MagicFlattenRemove]
        public decimal DummyLongValue {
            [MagicOrphan]
            get; 
            set; 
        }

        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude * (double)DummyLongValue
            };
            set
            {
                Latitude = (decimal)value.DumbNumber * 4;
            }
        }
    }

    /// <summary>
    /// A bad "get" that should fail
    /// </summary>
    [ShouldPass(false)]
    [MagicMap(typeof(IGeoLocation))]
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
    [MagicMap(typeof(IGeoLocation))]
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
    [MagicMap(typeof(IGeoLocation))]
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


    /// <summary>
    /// Removed the required, "Long" which is enforced from the IGeoLocation 
    /// and thus also has no getters or setters
    /// </summary>
    [ShouldPass(false)]
    [MagicMap(typeof(IGeoLocation))]
    public class GeoLocation_Fail4 : IGeoLocation
    {
        [MagicFlattenRemove]
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
                Longitude = (decimal)value.DumbNumber * 10;
                Latitude = (decimal)value.DumbNumber * 3;
            }
        }
    }

    /// <summary>
    /// DummyLongValue muddies Latitude in the set
    /// </summary>
    [ShouldPass(false)]
    [MagicMap(typeof(IGeoLocation))]
    public class GeoLocation_Fail5 : IGeoLocation
    {
        [MagicFlattenRemove]
        public decimal DummyLongValue { get; set; }

        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude * 4
            };
            set
            {
                Longitude = (decimal)value.DumbNumber * 10;
                Latitude = (decimal)value.DumbNumber * DummyLongValue;
            }
        }
    }

    /// <summary>
    /// No valid sets
    /// </summary>
    [ShouldPass(false)]
    [MagicMap(typeof(IGeoLocation))]
    public class GeoLocation_Fail6 : IGeoLocation
    {
        [MagicFlattenRemove]
        public decimal DummyLongValue { get; set; }

        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude * 4
            };
            set
            {
                DummyLongValue = (decimal)value.DumbNumber * 10;
            }
        }
    }

    /// <summary>
    /// DummyLongValue muddies the get
    /// </summary>
    [ShouldPass(false)]
    [MagicMap(typeof(IGeoLocation))]
    [TruthBindingTest]
    public class GeoLocation_Fail7 : IGeoLocation, IGeoLocationDup
    {
        [MagicFlattenRemove]
        public decimal DummyLongValue { get; set; }

        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude * (double)DummyLongValue
            };
            set
            {
                Latitude = (decimal)value.DumbNumber * 4;
            }
        }
    }


    [ShouldPass(false)]
    [MagicMap(typeof(IGeoLocation))]
    [MapImpTest]
    public class GeoLocation_Fail8 : IGeoLocationDup
    {
        [MagicFlattenRemove]
        public decimal DummyLongValue { get; set; }

        public decimal Longitude { get; set; }
        public decimal Latitude { get; set; }

        [MagicFlattenRemove]
        public DummyPoint Location
        {
            get => new DummyPoint()
            {
                DumbNumber = (double)Longitude * (double)DummyLongValue
            };
            set
            {
                Latitude = (decimal)value.DumbNumber * 4;
            }
        }
    }
}
