import { motion, type Variants } from 'motion/react';
import { useEffect, useState, type ReactNode } from 'react';
import { duration, ease } from '../motion';

const GAP_BETWEEN_ITEMS_SECONDS = 0.05;

const groupVariants: Variants = {
  visible: { transition: { staggerChildren: GAP_BETWEEN_ITEMS_SECONDS } },
};

const itemVariants: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: { opacity: 1, y: 0, transition: { duration: duration.slow, ease: ease.standard } },
};

let alreadyPlayed = false;

/** Reveals its items one after the other, the first time only: a returning user is not made to wait. */
export function StaggerGroup({ className, children }: { className?: string; children: ReactNode }) {
  const [playsNow] = useState(() => !alreadyPlayed);

  useEffect(() => {
    alreadyPlayed = true;
  }, []);

  return (
    <motion.div className={className} variants={groupVariants} initial={playsNow ? 'hidden' : false} animate="visible">
      {children}
    </motion.div>
  );
}

export function StaggerItem({ className, children }: { className?: string; children: ReactNode }) {
  return <motion.div className={className} variants={itemVariants}>{children}</motion.div>;
}
