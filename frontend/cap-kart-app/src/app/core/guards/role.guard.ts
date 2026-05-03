import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Route, Router, UrlSegment } from '@angular/router';
import { AuthService } from '../services/auth.service';

function isAllowed(role: string | null, allowedRoles: string[]): boolean {
  if (!role) return false;
  return allowedRoles.includes(role);
}

function redirectToAuth(router: Router): boolean {
  router.navigate(['/login']);
  return false;
}

export function roleGuard(allowedRoles: string[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isLoggedIn()) return redirectToAuth(router);
    if (isAllowed(auth.getRole(), allowedRoles)) return true;

    router.navigate(['/login']);
    return false;
  };
}

export function roleMatchGuard(allowedRoles: string[]): CanMatchFn {
  return (route: Route, segments: UrlSegment[]) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isLoggedIn()) return redirectToAuth(router);
    if (isAllowed(auth.getRole(), allowedRoles)) return true;

    router.navigate(['/login']);
    return false;
  };
}
