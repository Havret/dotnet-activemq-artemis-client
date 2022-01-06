import React from 'react';
import classnames from 'classnames';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import useBaseUrl from '@docusaurus/useBaseUrl';
import styles from './styles.module.css';

const features = [
  {
    title: <>.NET Standard compliant</>,
    imageUrl: 'img/dot-net-core.svg',
    description: (
      <>
        .NET ActiveMQ Artemis Client is .NET Standard 2.0 compliant and can be used in any .NET Core application.
      </>
    ),
  },
  {
    title: <>Asynchronous</>,
    imageUrl: 'img/undraw_synchronize_ccxk.svg',
    description: (
      <>
        .NET ActiveMQ Artemis Client was designed from the ground up to be fully asynchronous.
      </>
    ),
  },
  {
    title: <>Fast</>,
    imageUrl: 'img/undraw_To_the_stars_qhyy.svg',
    description: (
      <>
        .NET ActiveMQ Artemis Client is a very lightweight wrapper around AmqpNetLite and as such introduces very little overhead.
      </>
    ),
  },
];

function Feature({ imageUrl, title, description }) {
  const imgUrl = useBaseUrl(imageUrl);
  return (
    <div className={classnames('col col--4', styles.feature)}>
      {imgUrl && (
        <div className="text--center">
          <img className={styles.featureImage} src={imgUrl} alt={title} />
        </div>
      )}
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  );
}

function Home() {
  const context = useDocusaurusContext();
  const { siteConfig = {} } = context;
  return (
    <Layout
      title={siteConfig.title}
      description="Unofficial ActiveMQ Artemis .NET Client for .NET Core and .NET Framework">
      <header className={classnames('hero hero--primary', styles.heroBanner)}>
        <div className="container">
          <h1 className="hero__title">{siteConfig.title}</h1>
          <p className="hero__subtitle">{siteConfig.tagline}</p>
          <div className={styles.buttons}>
            <Link
              className={classnames(
                'button button--outline button--secondary button--lg',
                styles.getStarted,
              )}
              to={useBaseUrl('docs/getting-started')}>
              Get Started
            </Link>
          </div>
        </div>
      </header>
      <main>
        {features && features.length && (
          <section className={styles.features}>
            <div className="container">
              <div className="row">
                {features.map((props, idx) => (
                  <Feature key={idx} {...props} />
                ))}
              </div>
            </div>
          </section>
        )}
      </main>
    </Layout>
  );
}

export default Home;
