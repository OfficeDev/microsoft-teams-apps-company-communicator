import * as React from "react";

export const useInterval = (callback: any, delay: any) => {
  const savedCallback = React.useRef<any>();

  // Remember the latest callback.
  React.useEffect(() => {
    savedCallback.current = callback;
  }, [callback]);

  // Set up the interval.
  React.useEffect(() => {
    const tick = () => {
      savedCallback.current();
    };

    if (delay !== null) {
      /* tslint:disable-next-line */
      const id = setInterval(tick, delay);
      /* tslint:disable-next-line */
      return () => clearInterval(id);
    } else {
      return;
    }
  }, [delay]);
};
