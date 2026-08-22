import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of, map } from 'rxjs';
import { AuthUser } from '../models/dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private readonly apiBaseUrl = 'http://localhost:5103/api/v1/auth';

  currentUser = signal<AuthUser | null>(null);
  isLoading = signal<boolean>(false);

  isAuthenticated(): boolean {
    return this.currentUser() !== null;
  }

  login(email: string, password: string): Observable<AuthUser> {
    this.isLoading.set(true);
    return this.http.post<AuthUser>(
      `${this.apiBaseUrl}/login`,
      { email, password },
      { withCredentials: true }
    ).pipe(
      tap((user) => {
        this.currentUser.set(user);
        this.isLoading.set(false);
      }),
      catchError((err) => {
        this.isLoading.set(false);
        throw err;
      })
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(
      `${this.apiBaseUrl}/logout`,
      {},
      { withCredentials: true }
    ).pipe(
      tap(() => {
        this.currentUser.set(null);
        this.router.navigate(['/login']);
      }),
      catchError(() => {
        this.currentUser.set(null);
        this.router.navigate(['/login']);
        return of(undefined);
      })
    );
  }

  checkAuth(): Observable<boolean> {
    return this.http.get<AuthUser>(
      `${this.apiBaseUrl}/me`,
      { withCredentials: true }
    ).pipe(
      map((user) => {
        this.currentUser.set(user);
        return true;
      }),
      catchError(() => {
        this.currentUser.set(null);
        return of(false);
      })
    );
  }
}
