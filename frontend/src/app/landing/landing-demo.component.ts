import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ChatWidgetComponent } from '../widget/chat-widget/chat-widget.component';

export interface VerticalConfig {
  key: string;
  businessId: string;
  brandName: string;
  brandTag: string;
  headline: string;
  description: string;
  brandColor: string;
  greeting: string;
  ctaText: string;
  pricingText: string;
  stats: { number: string; label: string }[];
  featuresTitle: string;
  features: { icon: string; title: string; desc: string }[];
}

const VERTICALS: Record<string, VerticalConfig> = {
  tutoring: {
    key: 'tutoring',
    businessId: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    brandName: 'Bright Minds Coaching',
    brandTag: '🌟 Grades 8–12 STEM Excellence',
    headline: 'Master Math, Physics & CS with Elite Mentors',
    description: 'Personalized 1-on-1 tutoring and small group coaching designed to build rock-solid fundamentals, boost test scores, and cultivate a deep love for problem solving.',
    brandColor: '#4f46e5',
    greeting: "Hi! I'm the Bright Minds AI assistant. Ask me anything about our subjects, batch timings, trial classes, or fees!",
    ctaText: 'Book 100% Free Trial Class',
    pricingText: 'Explore Pricing ($35/hr)',
    stats: [
      { number: '98%', label: 'Grade Improvement' },
      { number: '6 : 1', label: 'Max Batch Size' },
      { number: '5+ Yrs', label: 'Avg Tutor Experience' }
    ],
    featuresTitle: 'Courses & Programs',
    features: [
      { icon: '📐', title: 'High School Mathematics', desc: 'Algebra, Geometry, Pre-Calc, AP Calculus AB/BC, and SAT Math prep.' },
      { icon: '⚡', title: 'Physics & Mechanics', desc: 'AP Physics 1 & C, Electromagnetism, and conceptual simulations.' },
      { icon: '🧪', title: 'Chemistry', desc: 'General, Honors, and AP Chemistry with stoichiometry coaching.' },
      { icon: '💻', title: 'Computer Science', desc: 'Python foundations, Java, AP Computer Science A, and algorithms.' }
    ]
  },
  dental: {
    key: 'dental',
    businessId: 'b2c3d4e5-f6a7-8901-bcde-f12345678901',
    brandName: 'Bright Smile Dental Clinic',
    brandTag: '✨ Gentle, Modern & Family-Centered Dentistry',
    headline: 'Advanced Dental Care & Radiant Smiles for the Whole Family',
    description: 'From routine preventive cleanings and pain-free restorative care to Invisalign clear aligners and laser teeth whitening — experience compassionate dental excellence.',
    brandColor: '#0d9488',
    greeting: 'Hello! Welcome to Bright Smile Dental Clinic. Ask me about our treatments, cleaning costs, accepted insurance, or appointments!',
    ctaText: 'Book Dental Exam ($120)',
    pricingText: 'View Treatments & Insurance',
    stats: [
      { number: '15+ Yrs', label: 'Clinical Excellence' },
      { number: '6 In-Net', label: 'Major Insurances' },
      { number: 'Same-Day', label: 'Emergency Slots' }
    ],
    featuresTitle: 'Clinical Services & Procedures',
    features: [
      { icon: '🦷', title: 'Preventive Care & Cleaning', desc: 'Comprehensive exams, low-dose digital X-rays, plaque removal, and oral cancer screenings.' },
      { icon: '✨', title: 'Cosmetic Teeth Whitening', desc: 'In-office 1-hour laser whitening ($350) and take-home custom trays.' },
      { icon: '😁', title: 'Invisalign Clear Aligners', desc: 'Free 3D digital scan with custom aligner treatment packages starting at $3,800.' },
      { icon: '🩹', title: 'Pain-Free Restorations', desc: 'Tooth-colored composite fillings, single-visit porcelain crowns, and gentle root canals.' }
    ]
  },
  realty: {
    key: 'realty',
    businessId: 'c3d4e5f6-a7b8-9012-cdef-123456789012',
    brandName: 'Skyline Realty Partners',
    brandTag: '🏙️ Premier Metro Residential & Commercial Brokerage',
    headline: 'Find Your Dream Home & Maximize Property Value',
    description: 'Full-service real estate advisory with over $250M in closed volume. Data-driven comparative valuations, luxury residential listings, and exclusive buyer representation.',
    brandColor: '#0284c7',
    greeting: 'Welcome to Skyline Realty Partners! How can we assist with your home purchase, property sale, or luxury rental?',
    ctaText: 'Request Free Home Valuation (CMA)',
    pricingText: 'Explore 5% Commission Structure',
    stats: [
      { number: '$250M+', label: 'Closed Transactions' },
      { number: '35 Days', label: 'Avg Sale Timeline' },
      { number: '0% Out-of-Pocket', label: 'For Home Buyers' }
    ],
    featuresTitle: 'Brokerage Services & Practice Areas',
    features: [
      { icon: '🏡', title: 'Buyer Representation', desc: 'Private accompanied tours, valuation, offer negotiation, and full escrow guidance.' },
      { icon: '📈', title: 'Seller Listings & Marketing', desc: 'HDR photography, 3D Matterport tours, MLS syndication, and open house hosting.' },
      { icon: '🏢', title: 'Commercial & Office Leasing', desc: 'Tenant and landlord representation for corporate, medical, and retail spaces.' },
      { icon: '🔑', title: 'Luxury Tenant Placement', desc: 'Comprehensive background/credit screening and move-in inspection reports.' }
    ]
  }
};

@Component({
  selector: 'nml-landing-demo',
  standalone: true,
  imports: [CommonModule, RouterModule, ChatWidgetComponent],
  templateUrl: './landing-demo.component.html',
  styleUrl: './landing-demo.component.css'
})
export class LandingDemoComponent implements OnInit {
  private route = inject(ActivatedRoute);

  currentConfig = signal<VerticalConfig>(VERTICALS['tutoring']);

  ngOnInit() {
    this.route.data.subscribe((data) => {
      const verticalKey = data['vertical'] || 'tutoring';
      this.currentConfig.set(VERTICALS[verticalKey] || VERTICALS['tutoring']);
    });
  }
}
