import { HttpInterceptorFn } from '@angular/common/http';

export const csrfHeaderInterceptor: HttpInterceptorFn = (req, next) => {
  const modifiedRequest = req.clone({
    withCredentials: true, // ensure cookies are included if needed
    headers: req.headers.set('X-CSRF', '1')
  });
  return next(modifiedRequest);
};
