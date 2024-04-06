using Torch;
using Torch.Views;

namespace Sclean
{
    public class ScleanConfig : ViewModel
    {
        private string _beaconSubtype = "ScrapBeacon";
        [Display(Name = "Beacon Subtype name", Description = "Beacon SubtypeId ends with this")]
        public string BeaconSubtype { get => _beaconSubtype; set => SetValue(ref _beaconSubtype, value); }

        private int _playerRange = 10000;
        [Display(Name = "Player", GroupName = "Protection Range", Description = "Radius of the protection AOE")]
        public int PlayerRange { get => _playerRange; set => SetValue(ref _playerRange, value); }

        private int _scrapBeaconRange = 250;
        [Display(Name = "Scrap Beacon", GroupName = "Protection Range", Description = "Radius of the protection AOE")]
        public int ScrapBeaconRange { get => _scrapBeaconRange; set => SetValue(ref _scrapBeaconRange, value); }
    }
}
