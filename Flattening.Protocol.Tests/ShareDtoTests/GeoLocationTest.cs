using Flattening.Protocol.Tests.Attributes;
using Magic.Flattening.Toolkit.Attributes;
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
