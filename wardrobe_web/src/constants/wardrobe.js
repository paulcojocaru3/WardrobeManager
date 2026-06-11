import { COLOR_HEX } from './colors';

export const CLOTHING_TYPES = ['TOP', 'BOTTOM', 'SHOES', 'OUTERWEAR', 'ACCESSORY'];

export const SEASONS = ['Summer', 'Fall', 'Winter', 'Spring', 'All Seasons'];

export const USAGES = ['Casual', 'Ethnic', 'Formal', 'Party', 'Smart Casual', 'Sports', 'Travel'];

export const EVENT_MOMENTS = [
  'Morning', 
  'Day / City Walk', 
  'Afternoon', 
  'Evening', 
  'Dinner', 
  'Party / Night', 
  'Flight / Travel', 
  'Gym / Workout', 
  'Business / Meeting', 
  'Date / Romantic', 
  'Ceremony / Wedding'
];

// Derived from the canonical color → hex map so the dropdown options and the
// swatch colors can never drift apart. Order is preserved from COLOR_HEX.
export const COLORS = Object.keys(COLOR_HEX);
