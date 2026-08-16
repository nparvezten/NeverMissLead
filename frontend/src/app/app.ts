import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatWidgetComponent } from './widget/chat-widget/chat-widget.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ChatWidgetComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  title = signal('Bright Minds Coaching');
  businessId = signal('a1b2c3d4-e5f6-7890-abcd-ef1234567890');
}
