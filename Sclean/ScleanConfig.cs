using Torch;
using Torch.Views;

namespace Sclean
{
    public class ScleanConfig : ViewModel
    {
        private int _playerRange = 250;
        [Display(Name = "Player", GroupName = "Protection Range", Description = "Radius of the protection AOE")]
        public int PlayerRange { get => _playerRange; set => SetValue(ref _playerRange, value); }

        private int _smallScrapBeacon = 250;
        [Display(Name = "Small Scrap Beacon", GroupName = "Protection Range", Description = "Radius of the protection AOE")]
        public int SmallScrapBeacon { get => _smallScrapBeacon; set => SetValue(ref _smallScrapBeacon, value); }

        private int _largeScrapBeacon = 250;
        [Display(Name = "Large Scrap Beacon", GroupName = "Protection Range", Description = "Radius of the protection AOE")]
        public int LargeScrapBeacon { get => _largeScrapBeacon; set => SetValue(ref _largeScrapBeacon, value); }
    }
}
