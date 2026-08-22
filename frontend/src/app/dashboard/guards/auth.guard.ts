import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { map } from 'rxjs';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return authService.checkAuth().pipe(
    map((isAuth) => {
      if (isAuth) {
        return true;
      }
      return router.createUrlTree(['/login']);
    })
  );
};
