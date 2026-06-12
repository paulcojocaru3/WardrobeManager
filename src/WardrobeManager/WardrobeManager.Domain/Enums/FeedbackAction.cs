namespace WardrobeManager.Domain.Enums;

// what the user did with a recommended item — drives the training labels
public enum FeedbackAction
{
    Shown,      // logged at generation time, no decision yet
    Accepted,   // kept in the saved outfit (positive)
    Rejected,   // shown but not chosen (negative)
    Worn,       // later worn (positive)
    Favorited   // later favorited (positive)
}
