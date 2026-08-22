import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'nml-login',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = signal('owner@brightminds.test');
  password = signal('BrightMinds2026!');
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  fillDemo(email: string, password: string) {
    this.email.set(email);
    this.password.set(password);
    this.errorMessage.set(null);
  }

  onSubmit() {
    if (!this.email() || !this.password()) {
      this.errorMessage.set('Please enter both email and password.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.email(), this.password()).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        const detail = err.error?.detail || err.error?.title || 'Invalid credentials or connection error.';
        this.errorMessage.set(detail);
      }
    });
  }
}
