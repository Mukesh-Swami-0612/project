import { HttpErrorResponse, HttpEvent, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

function extractMessage(err: HttpErrorResponse): string {
  const anyErr: any = err.error;
  if (typeof anyErr === 'string') return anyErr;
  if (anyErr?.message) return anyErr.message;
  if (anyErr?.title) return anyErr.title;
  return err.message || 'Request failed.';
}

export const httpErrorInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // status 0 = network error (no server response) — must be checked BEFORE the >= 400 block
      if (err.status === 0) {
        toast.show('Network error. Is the API running?', 'error');
      } else if (err.status >= 400) {
        const msg = extractMessage(err);
        if (err.status === 401) toast.show(msg || 'Unauthorized. Please login again.', 'warning');
        else if (err.status === 403) toast.show(msg || 'Forbidden. You do not have access.', 'error');
        else toast.show(msg, 'error');
      }
      return throwError(() => err);
    })
  );
};
