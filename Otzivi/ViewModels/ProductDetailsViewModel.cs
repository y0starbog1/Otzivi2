using Otzivi.Models;

namespace Otzivi.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; }
        public List<Review> Reviews { get; set; }
        public bool HasUserReviewed { get; set; }
        public CreateReviewViewModel NewReview { get; set; }
    }
}