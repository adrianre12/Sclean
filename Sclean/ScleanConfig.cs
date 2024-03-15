using Torch;
using Torch.Views;

namespace Sclean
{
    public class ScleanConfig : ViewModel
    {
        private int _playerRange = 250;
        [Display(Name = "Player", GroupName = "Protection Range", Description = "Radius of the protection AOE")]
        public int PlayerRange { get => _playerRange; set => SetValue(ref _playerRange, value); }

        private int _scrapBeaconRange = 250;
        [Display(Name = "Scrap Beacon", GroupName = "Protection Range", Description = "Radius of the protection AOE")]
        public int ScrapBeaconRange { get => _scrapBeaconRange; set => SetValue(ref _scrapBeaconRange, value); }
    }
}
