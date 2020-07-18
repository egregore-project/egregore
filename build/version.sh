#!/bin/bash

# Get the latest tag
tag=$(git describe --tags `git rev-list --tags --max-count=1`)
tag_commit=$(git rev-list -n 1 $tag)

# Get current commit hash for tag
commit=$(git rev-parse HEAD)

if [ "$tag_commit" == "$commit" ]; then
    echo "No new commits since previous tag.."
    exit 1
fi

if [ ! -f "./build/version.json" ]; then
    echo "Could not find version.json file.."
    exit 1
fi

# Prefix release with 'v'
new="v$NBGV_Version"
echo ::set-output name=new_tag::$new

# Push new tag ref to GitHub
dt=$(date '+%Y-%m-%dT%H:%M:%SZ')
full_name=$GITHUB_REPOSITORY
git_refs_url=$(jq .repository.git_refs_url $GITHUB_EVENT_PATH | tr -d '"' | sed 's/{\/sha}//g')

echo "$dt: pushing tag $new to repo $full_name"
echo $git_refs_url
cat $GITHUB_EVENT_PATH | jq '.'

curl -s -X POST $git_refs_url \
-H "Authorization: token ${{ secrets.GITHUB_TOKEN }}" \
-d @- << EOF

{
  "ref": "refs/tags/$new",
  "sha": "$commit"
}
EOF
