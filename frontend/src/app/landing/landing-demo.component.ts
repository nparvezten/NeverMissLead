import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ChatWidgetComponent } from '../widget/chat-widget/chat-widget.component';

@Component({
  selector: 'nml-landing-demo',
  standalone: true,
  imports: [CommonModule, RouterModule, ChatWidgetComponent],
  templateUrl: './landing-demo.component.html',
  styleUrl: './landing-demo.component.css'
})
export class LandingDemoComponent {
  businessId = signal('a1b2c3d4-e5f6-7890-abcd-ef1234567890');
}
